using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace Backend.Authorization;

public sealed class JsonAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Forbidden)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";

            var path = context.Request.Path.Value ?? string.Empty;
            var isSubscriptionApi = path.StartsWith("/api/subscriptions", StringComparison.OrdinalIgnoreCase);
            var failedRequirements = authorizeResult.AuthorizationFailure?.FailedRequirements;
            var failedSubscriptionTier = failedRequirements?.Any(r => r is ActiveSubscriptionRequirement or PackageFeatureRequirement) == true;

            var message = context.User.Identity?.IsAuthenticated != true
                ? "Bạn không có quyền truy cập tính năng này."
                : isSubscriptionApi && !failedSubscriptionTier
                    ? "Bạn không có quyền quản lý gói dịch vụ với tài khoản này."
                    : "Tính năng này chưa được mở trong gói dịch vụ của bạn. Vui lòng nâng cấp gói hoặc liên hệ quản trị viên.";

            await context.Response.WriteAsJsonAsync(new
            {
                message,
                code = "FORBIDDEN"
            });
            return;
        }

        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}
