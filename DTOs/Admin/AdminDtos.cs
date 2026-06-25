namespace Backend.DTOs.Admin;

public class PagedResultDto<T>
{
    public IEnumerable<T> Items { get; set; } = Array.Empty<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
}

// Dashboard
public class AdminDashboardSummaryDto
{
    public int TotalOwners { get; set; }
    public int ActiveOwners { get; set; }
    public int ExpiredOwners { get; set; }
    public int SuspendedOwners { get; set; }
    public int TotalTenants { get; set; }
    public int TotalRooms { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public decimal AnnualRevenue { get; set; }
    public decimal Mrr { get; set; }
    public decimal Arr { get; set; }
}

public class ChartDataPointDto
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public int Count { get; set; }
}

public class AdminDashboardChartsDto
{
    public List<ChartDataPointDto> RevenueGrowth { get; set; } = new();
    public List<ChartDataPointDto> PackageDistribution { get; set; } = new();
    public List<ChartDataPointDto> OwnerGrowth { get; set; } = new();
    public List<ChartDataPointDto> SubscriptionStatus { get; set; } = new();
    public List<ChartDataPointDto> PaymentMethods { get; set; } = new();
    public List<ChartDataPointDto> PlatformAdoption { get; set; } = new();
    public List<ChartDataPointDto> ExpiringTimeline { get; set; } = new();
    public List<AdminDashboardAlertDto> Alerts { get; set; } = new();
}

public class AdminDashboardAlertDto
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Count { get; set; }
    public string? ActionPath { get; set; }
}

// Owners
public class AdminOwnerListDto
{
    public int OwnerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Avatar { get; set; }
    public string? Package { get; set; }
    public string SubscriptionStatus { get; set; } = "None";
    public DateTime CreatedDate { get; set; }
    public DateTime? ExpiredDate { get; set; }
    public bool IsActive { get; set; }
    public bool IsSuspended { get; set; }
    public int RoomCount { get; set; }
    public int BuildingCount { get; set; }
}

public class AdminOwnerDetailDto : AdminOwnerListDto
{
    public int? PackageId { get; set; }
    public int? SubscriptionId { get; set; }
    public DateTime? SubscriptionStartDate { get; set; }
    public string? CCCD { get; set; }
    public string? Address { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateAdminOwnerDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int? PackageId { get; set; }
}

public class UpdateAdminOwnerDto
{
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public int? PackageId { get; set; }
}

public class OwnerFeatureGrantItemDto
{
    public string Feature { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string RequiredPackage { get; set; } = string.Empty;
    public bool IncludedInPackage { get; set; }
    public bool ManuallyGranted { get; set; }
    public bool IsEffective { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Note { get; set; }
}

public class OwnerFeatureGrantsDto
{
    public int OwnerId { get; set; }
    public string? PackageName { get; set; }
    public List<OwnerFeatureGrantItemDto> Features { get; set; } = [];
}

public class OwnerFeatureGrantUpdateItemDto
{
    public string Feature { get; set; } = string.Empty;
    public bool Granted { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Note { get; set; }
}

public class UpdateOwnerFeatureGrantsDto
{
    public List<OwnerFeatureGrantUpdateItemDto> Grants { get; set; } = [];
}

// Packages
public class AdminPackageDto
{
    public int PackageId { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int MaxRooms { get; set; }
    public string? Description { get; set; }
    public string? RoomRange { get; set; }
    public string? TargetAudience { get; set; }
    public bool IsRecommended { get; set; }
    public List<string> Features { get; set; } = [];
    public bool IsEnabled { get; set; }
    public int SubscriberCount { get; set; }
}

public class CreatePackageDto
{
    public string PackageName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int MaxRooms { get; set; }
    public string? Description { get; set; }
    public string? RoomRange { get; set; }
    public string? TargetAudience { get; set; }
    public bool IsRecommended { get; set; }
    public List<string>? Features { get; set; }
}

public class UpdatePackageDto : CreatePackageDto { }

// Subscriptions
public class AdminSubscriptionDto
{
    public int SubscriptionId { get; set; }
    public int OwnerId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public int PackageId { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int OwnerRoomCount { get; set; }
    public int MaxRooms { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminOwnerSubscriptionsGroupDto
{
    public int OwnerId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public string? OwnerEmail { get; set; }
    public int OwnerRoomCount { get; set; }
    public List<AdminSubscriptionDto> Subscriptions { get; set; } = [];
}

public class ChangePackageDto
{
    public int PackageId { get; set; }
}

// Payments
public class AdminPaymentDto
{
    public int PaymentId { get; set; }
    public int OwnerId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public int SubscriptionId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class RevenueReportDto
{
    public decimal TotalRevenue { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public int TotalPayments { get; set; }
    public List<ChartDataPointDto> ByMonth { get; set; } = new();
}

// Users
public class AdminUserDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? Avatar { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsSuspended { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Package { get; set; }
    public string? SubscriptionStatus { get; set; }
    public DateTime? ExpiredDate { get; set; }
    public int RoomCount { get; set; }
    public int BuildingCount { get; set; }
}

public class AdminResetPasswordResultDto
{
    public string Message { get; set; } = string.Empty;
    public string TemporaryPassword { get; set; } = string.Empty;
}

public class AdminUserPasswordDto
{
    public string? Password { get; set; }
}

public class AdminChangePasswordDto
{
    public string NewPassword { get; set; } = string.Empty;
}

// Audit Logs
public class AdminAuditLogDto
{
    public int LogId { get; set; }
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Entity { get; set; }
    public int? EntityId { get; set; }
    public string? IPAddress { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Details { get; set; }
}
