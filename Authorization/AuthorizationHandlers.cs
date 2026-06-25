using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Backend.Data;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Backend.Authorization;

public sealed class ActiveUserRequirement : IAuthorizationRequirement;

public sealed class NotSuspendedRequirement : IAuthorizationRequirement;

public sealed class ActiveSubscriptionRequirement : IAuthorizationRequirement;

public sealed class PackageFeatureRequirement(PackageFeature feature) : IAuthorizationRequirement
{
    public PackageFeature Feature { get; } = feature;
}

public sealed class OwnerRoleRequirement : IAuthorizationRequirement;

public class OwnerRoleAuthorizationHandler : AuthorizationHandler<OwnerRoleRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OwnerRoleRequirement requirement)
    {
        if (AuthorizationClaimExtensions.IsInRole(context.User, RoleNames.Owner))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}

public class ActiveUserAuthorizationHandler : AuthorizationHandler<ActiveUserRequirement>
{
    private readonly RentalManagementDb _context;

    public ActiveUserAuthorizationHandler(RentalManagementDb context)
    {
        _context = context;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ActiveUserRequirement requirement)
    {
        var userId = AuthorizationClaimExtensions.GetUserId(context.User);
        if (userId == null) return;

        var isActive = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId.Value)
            .Select(u => u.IsActive)
            .FirstOrDefaultAsync();

        if (isActive)
            context.Succeed(requirement);
    }
}

public class NotSuspendedAuthorizationHandler : AuthorizationHandler<NotSuspendedRequirement>
{
    private readonly RentalManagementDb _context;

    public NotSuspendedAuthorizationHandler(RentalManagementDb context)
    {
        _context = context;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        NotSuspendedRequirement requirement)
    {
        var userId = AuthorizationClaimExtensions.GetUserId(context.User);
        if (userId == null) return;

        var isSuspended = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId.Value)
            .Select(u => u.IsSuspended)
            .FirstOrDefaultAsync();

        if (!isSuspended)
            context.Succeed(requirement);
    }
}

public class ActiveSubscriptionAuthorizationHandler : AuthorizationHandler<ActiveSubscriptionRequirement>
{
    private readonly RentalManagementDb _context;

    public ActiveSubscriptionAuthorizationHandler(RentalManagementDb context)
    {
        _context = context;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ActiveSubscriptionRequirement requirement)
    {
        var userId = AuthorizationClaimExtensions.GetUserId(context.User);
        if (userId == null) return;

        var now = DateTime.Now;
        var hasActiveSubscription = await _context.Subscriptions
            .AsNoTracking()
            .AnyAsync(s =>
                s.OwnerUserId == userId.Value &&
                s.Status == "Active" &&
                s.EndDate >= now);

        if (hasActiveSubscription)
            context.Succeed(requirement);
    }
}

public class PackageFeatureAuthorizationHandler : AuthorizationHandler<PackageFeatureRequirement>
{
    private readonly IOwnerFeatureService _ownerFeatureService;

    public PackageFeatureAuthorizationHandler(IOwnerFeatureService ownerFeatureService)
    {
        _ownerFeatureService = ownerFeatureService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PackageFeatureRequirement requirement)
    {
        var userId = AuthorizationClaimExtensions.GetUserId(context.User);
        if (userId == null) return;

        if (await _ownerFeatureService.HasFeatureAsync(userId.Value, requirement.Feature))
            context.Succeed(requirement);
    }
}

public static class AuthorizationClaimExtensions
{
    public static int? GetUserId(ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(claim, out var userId) ? userId : null;
    }

    public static bool IsInRole(ClaimsPrincipal user, string role) =>
        user.IsInRole(role)
        || user.Claims.Any(c =>
            (c.Type == ClaimTypes.Role || c.Type == "role") &&
            string.Equals(c.Value, role, StringComparison.OrdinalIgnoreCase));
}
