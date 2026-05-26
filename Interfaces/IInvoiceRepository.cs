using Backend.Entities;

namespace Backend.Interfaces;

public interface IInvoiceRepository
{
    Task<Room?> GetRoomWithDetailsAsync(int roomId);
    Task<UtilityUsage?> GetUtilityUsageAsync(int roomId, string monthYear);
    Task<User?> GetUserByIdAsync(int userId);
    Task<User?> GetAnyUserAsync();
    Task<Invoice?> GetInvoiceByRoomAndMonthAsync(int roomId, string monthYear);
    Task<Invoice?> GetInvoiceByIdAsync(int invoiceId);
    Task<IEnumerable<Invoice>> SearchInvoicesAsync(int? roomId = null, string? tenantName = null, string? monthYearFrom = null, string? monthYearTo = null, string? status = null, string? search = null);
    Task<IEnumerable<Room>> GetRoomsWithDetailsAsync(int? buildingId = null);
    Task AddUtilityUsageAsync(UtilityUsage usage);
    Task UpdateUtilityUsageAsync(UtilityUsage usage);
    Task AddInvoiceAsync(Invoice invoice);
    Task UpdateInvoiceAsync(Invoice invoice);
    Task SaveChangesAsync();
}
