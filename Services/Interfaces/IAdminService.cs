using Backend.DTOs.Admin;
using Backend.DTOs.Package;

namespace Backend.Services.Interfaces;

public interface IAdminService
{
    Task<AdminDashboardSummaryDto> GetDashboardSummaryAsync();
    Task<AdminDashboardChartsDto> GetDashboardChartsAsync();

    Task<PagedResultDto<AdminOwnerListDto>> GetOwnersAsync(string? search, string? status, int page, int pageSize);
    Task<AdminOwnerDetailDto?> GetOwnerByIdAsync(int ownerId);
    Task<AdminOwnerDetailDto> CreateOwnerAsync(CreateAdminOwnerDto dto, int? adminUserId, string? ip);
    Task<AdminOwnerDetailDto> UpdateOwnerAsync(int ownerId, UpdateAdminOwnerDto dto, int? adminUserId, string? ip);
    Task DeleteOwnerAsync(int ownerId, int? adminUserId, string? ip);
    Task<AdminOwnerDetailDto> SuspendOwnerAsync(int ownerId, int? adminUserId, string? ip);
    Task<AdminOwnerDetailDto> ActivateOwnerAsync(int ownerId, int? adminUserId, string? ip);
    Task<AdminOwnerDetailDto> LockOwnerAsync(int ownerId, int? adminUserId, string? ip);
    Task<AdminOwnerDetailDto> UnlockOwnerAsync(int ownerId, int? adminUserId, string? ip);

    Task<PagedResultDto<AdminPackageDto>> GetPackagesAsync(string? search, bool? isEnabled, int page, int pageSize);
    Task<AdminPackageDto> CreatePackageAsync(CreatePackageDto dto, int? adminUserId, string? ip);
    Task<AdminPackageDto> UpdatePackageAsync(int packageId, UpdatePackageDto dto, int? adminUserId, string? ip);
    Task<AdminPackageDto> EnablePackageAsync(int packageId, int? adminUserId, string? ip);
    Task<AdminPackageDto> DisablePackageAsync(int packageId, int? adminUserId, string? ip);
    Task DeletePackageAsync(int packageId, int? adminUserId, string? ip);

    Task<PagedResultDto<AdminSubscriptionDto>> GetSubscriptionsAsync(string? status, string? search, int page, int pageSize);
    Task<PagedResultDto<AdminOwnerSubscriptionsGroupDto>> GetSubscriptionsGroupedByOwnerAsync(string? status, string? search, int page, int pageSize);
    Task<AdminSubscriptionDto> UpgradeSubscriptionAsync(int subscriptionId, ChangePackageDto dto, int? adminUserId, string? ip);
    Task<AdminSubscriptionDto> DowngradeSubscriptionAsync(int subscriptionId, ChangePackageDto dto, int? adminUserId, string? ip);
    Task<AdminSubscriptionDto> RenewSubscriptionAsync(int subscriptionId, int? adminUserId, string? ip);
    Task<AdminSubscriptionDto> CancelSubscriptionAsync(int subscriptionId, int? adminUserId, string? ip);
    Task<AdminSubscriptionDto> ActivateSubscriptionAsync(int subscriptionId, int? adminUserId, string? ip);
    Task DeleteSubscriptionAsync(int subscriptionId, int? adminUserId, string? ip);

    Task<PagedResultDto<AdminPaymentDto>> GetPaymentsAsync(string? status, int? ownerId, DateTime? from, DateTime? to, int page, int pageSize);
    Task<RevenueReportDto> GetRevenueReportAsync(DateTime? from, DateTime? to);
    Task<byte[]> ExportPaymentsExcelAsync(DateTime? from, DateTime? to);

    Task<PagedResultDto<AdminUserDto>> GetUsersAsync(string? role, string? search, bool? isActive, string? subscriptionStatus, int page, int pageSize);
    Task<AdminUserDto> EnableUserAsync(int userId, int? adminUserId, string? ip);
    Task<AdminUserDto> DisableUserAsync(int userId, int? adminUserId, string? ip);
    Task<AdminResetPasswordResultDto> ResetUserPasswordAsync(int userId, int? adminUserId, string? ip);
    Task<AdminUserPasswordDto> GetUserPasswordAsync(int userId);
    Task ChangeUserPasswordAsync(int userId, AdminChangePasswordDto dto, int? adminUserId, string? ip);
    Task DeleteUserAsync(int userId, int? adminUserId, string? ip);

    Task<PagedResultDto<AdminAuditLogDto>> GetAuditLogsAsync(int? userId, string? action, string? entity, DateTime? from, DateTime? to, int page, int pageSize);
    Task<int> ClearAuditLogsAsync(int? userId, string? action, string? entity, DateTime? from, DateTime? to, int? adminUserId, string? ip);

    Task<PlatformPaymentSettingDto> GetPlatformPaymentSettingsAsync();
    Task<PlatformPaymentSettingDto> UpdatePlatformPaymentSettingsAsync(UpdatePlatformPaymentSettingDto dto, int? adminUserId, string? ip);
}
