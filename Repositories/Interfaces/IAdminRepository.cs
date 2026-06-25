using Backend.DTOs.Admin;
using Backend.Entities;

namespace Backend.Repositories.Interfaces;

public interface IAdminRepository
{
    Task<int> GetOwnerRoleIdAsync();
    Task<AdminDashboardSummaryDto> GetDashboardSummaryAsync();
    Task<AdminDashboardChartsDto> GetDashboardChartsAsync();

    Task<(List<AdminOwnerListDto> Items, int Total)> GetOwnersAsync(string? search, string? status, int page, int pageSize);
    Task<AdminOwnerDetailDto?> GetOwnerByIdAsync(int ownerId);
    Task<User?> GetOwnerEntityAsync(int ownerId);
    Task<int> GetOwnerRoomCountAsync(int ownerUserId);
    Task CleanupOwnerBeforeDeleteAsync(int ownerUserId);

    Task<(List<AdminPackageDto> Items, int Total)> GetPackagesAsync(string? search, bool? isEnabled, int page, int pageSize);
    Task<Package?> GetPackageByIdAsync(int packageId);
    Task AddPackageAsync(Package package);
    Task DeletePackageAsync(Package package);
    Task<bool> PackageHasSubscriptionsAsync(int packageId);

    Task<(List<AdminSubscriptionDto> Items, int Total)> GetSubscriptionsAsync(string? status, string? search, int page, int pageSize);
    Task<(List<AdminOwnerSubscriptionsGroupDto> Items, int Total)> GetSubscriptionsGroupedByOwnerAsync(string? status, string? search, int page, int pageSize);
    Task<Subscription?> GetSubscriptionByIdAsync(int subscriptionId);
    Task<Subscription?> GetActiveSubscriptionAsync(int ownerUserId);
    Task AddSubscriptionAsync(Subscription subscription);
    Task DeleteSubscriptionAsync(Subscription subscription);
    Task ExpireSubscriptionsAsync();

    Task<(List<AdminPaymentDto> Items, int Total)> GetPaymentsAsync(string? status, int? ownerId, DateTime? from, DateTime? to, int page, int pageSize);
    Task<RevenueReportDto> GetRevenueReportAsync(DateTime? from, DateTime? to);
    Task AddPaymentAsync(SubscriptionPayment payment);

    Task<(List<AdminUserDto> Items, int Total)> GetUsersAsync(string? role, string? search, bool? isActive, string? subscriptionStatus, int page, int pageSize);
    Task<User?> GetUserByIdAsync(int userId);

    Task<(List<AdminAuditLogDto> Items, int Total)> GetAuditLogsAsync(int? userId, string? action, string? entity, DateTime? from, DateTime? to, int page, int pageSize);
    Task<int> ClearAuditLogsAsync(int? userId, string? action, string? entity, DateTime? from, DateTime? to);
    Task AddAuditLogAsync(AuditLog log);

    Task<int> CountTenantsAsync();
    Task<int> CountRoomsAsync();
    Task SaveChangesAsync();
}
