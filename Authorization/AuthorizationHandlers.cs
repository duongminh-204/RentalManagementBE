using System.Security.Claims;
using Backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Backend.Authorization;

public sealed class ActiveUserRequirement : IAuthorizationRequirement;

public sealed class NotSuspendedRequirement : IAuthorizationRequirement;

public sealed class ActiveSubscriptionRequirement : IAuthorizationRequirement;

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

internal static class AuthorizationClaimExtensions
{
    internal static int? GetUserId(ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var userId) ? userId : null;
    }
}
