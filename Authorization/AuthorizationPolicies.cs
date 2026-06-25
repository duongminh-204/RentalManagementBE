namespace Backend.Authorization;

public static class AuthorizationPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string OwnerOrAdmin = "OwnerOrAdmin";
    public const string OwnerOnly = "OwnerOnly";
    public const string TenantOnly = "TenantOnly";
    public const string ActiveUser = "ActiveUser";
    public const string ActiveOwner = "ActiveOwner";
    public const string ActiveOwnerSubscription = "ActiveOwnerSubscription";
}

public static class RoleNames
{
    public const string Admin = "Admin";
    public const string Owner = "Owner";
    public const string Tenant = "Tenant";
}
