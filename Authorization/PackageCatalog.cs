namespace Backend.Authorization;

public enum PackageFeature
{
    Dashboard,
    TenantManagement,
    UtilitiesInvoices,
    Contracts,
    PaymentInvoices,
    RevenueDebtReports,
    VehicleManagement,
    AiRoomDecor,
    LegalChecklist
}

public static class PackageCatalog
{
    public const string Starter = "Starter";
    public const string Pro = "PRO";
    public const string Premium = "PREMIUM";

    public sealed record PackageDefinition(
        string PackageName,
        string RoomRange,
        string TargetAudience,
        decimal Price,
        int MaxRooms,
        string Description,
        bool Recommended,
        IReadOnlyList<string> FeatureLines,
        IReadOnlySet<PackageFeature> Features);

    private static readonly PackageDefinition[] Definitions =
    [
        new(
            Starter,
            "<=20 phòng",
            "Chủ trọ non-tech",
            149_000m,
            20,
            "Gói khởi đầu cho chủ trọ quy mô nhỏ",
            false,
            [
                "Dashboard dữ liệu",
                "Quản lý khách thuê",
                "Quản lý điện nước & hoá đơn",
                "Quản lý hợp đồng",
                "Tạo hoá đơn thanh toán"
            ],
            new HashSet<PackageFeature>
            {
                PackageFeature.Dashboard,
                PackageFeature.TenantManagement,
                PackageFeature.UtilitiesInvoices,
                PackageFeature.Contracts,
                PackageFeature.PaymentInvoices
            }),
        new(
            Pro,
            "21-50 phòng",
            "Chủ trọ đang mở rộng",
            299_000m,
            50,
            "Gói PRO — khuyên dùng cho chủ trọ đang mở rộng",
            true,
            [
                "Toàn bộ tính năng Starter",
                "Báo cáo doanh thu & công nợ",
                "Quản lý phương tiện người thuê",
                "Hỗ trợ 24/7"
            ],
            new HashSet<PackageFeature>
            {
                PackageFeature.Dashboard,
                PackageFeature.TenantManagement,
                PackageFeature.UtilitiesInvoices,
                PackageFeature.Contracts,
                PackageFeature.PaymentInvoices,
                PackageFeature.RevenueDebtReports,
                PackageFeature.VehicleManagement
            }),
        new(
            Premium,
            "51-100 phòng",
            "Quản lý chuyên nghiệp",
            599_000m,
            100,
            "Gói PREMIUM cho quản lý chuyên nghiệp",
            false,
            [
                "Toàn bộ tính năng Pro",
                "AI decor phòng",
                "Checklist pháp lý"
            ],
            new HashSet<PackageFeature>
            {
                PackageFeature.Dashboard,
                PackageFeature.TenantManagement,
                PackageFeature.UtilitiesInvoices,
                PackageFeature.Contracts,
                PackageFeature.PaymentInvoices,
                PackageFeature.RevenueDebtReports,
                PackageFeature.VehicleManagement,
                PackageFeature.AiRoomDecor,
                PackageFeature.LegalChecklist
            })
    ];

    public static IReadOnlyList<PackageDefinition> All => Definitions;

    public static PackageDefinition? Find(string? packageName) =>
        Definitions.FirstOrDefault(p =>
            string.Equals(p.PackageName, packageName, StringComparison.OrdinalIgnoreCase));

    public static bool HasFeature(string? packageName, PackageFeature feature)
    {
        var package = Find(packageName);
        return package?.Features.Contains(feature) == true;
    }

    public static IReadOnlySet<PackageFeature> GetPackageFeatures(string? packageName)
    {
        var package = Find(packageName);
        return package?.Features ?? new HashSet<PackageFeature>();
    }

    public static string GetDisplayName(PackageFeature feature) => feature switch
    {
        PackageFeature.Dashboard => "Dashboard dữ liệu",
        PackageFeature.TenantManagement => "Quản lý khách thuê",
        PackageFeature.UtilitiesInvoices => "Quản lý điện nước & hoá đơn",
        PackageFeature.Contracts => "Quản lý hợp đồng",
        PackageFeature.PaymentInvoices => "Tạo hoá đơn thanh toán",
        PackageFeature.RevenueDebtReports => "Báo cáo doanh thu & công nợ",
        PackageFeature.VehicleManagement => "Quản lý phương tiện",
        PackageFeature.AiRoomDecor => "AI decor phòng",
        PackageFeature.LegalChecklist => "Checklist pháp lý",
        _ => feature.ToString()
    };

    public static string GetRequiredPackageName(PackageFeature feature) => feature switch
    {
        PackageFeature.RevenueDebtReports or PackageFeature.VehicleManagement => Pro,
        PackageFeature.AiRoomDecor or PackageFeature.LegalChecklist => Premium,
        _ => Starter
    };

    public static string GetPolicyName(PackageFeature feature) => $"PackageFeature.{feature}";
}
