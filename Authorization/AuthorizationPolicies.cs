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

public static class PackageFeaturePolicies
{
    public const string Dashboard = "PackageFeature.Dashboard";
    public const string TenantManagement = "PackageFeature.TenantManagement";
    public const string UtilitiesInvoices = "PackageFeature.UtilitiesInvoices";
    public const string Contracts = "PackageFeature.Contracts";
    public const string PaymentInvoices = "PackageFeature.PaymentInvoices";
    public const string RevenueDebtReports = "PackageFeature.RevenueDebtReports";
    public const string VehicleManagement = "PackageFeature.VehicleManagement";
    public const string AiRoomDecor = "PackageFeature.AiRoomDecor";
    public const string LegalChecklist = "PackageFeature.LegalChecklist";
}
