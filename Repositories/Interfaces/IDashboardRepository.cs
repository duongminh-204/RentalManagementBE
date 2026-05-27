using Backend.DTOs.Dashboard;
using Backend.Entities;

namespace Backend.Repositories.Interfaces;

public interface IDashboardRepository
{
    Task<List<DashboardRoomStatusRecordDto>> GetRoomStatusesAsync(int? buildingId = null);
    Task<List<DashboardDebtRecordDto>> GetDebtRecordsAsync(int? buildingId = null);
    Task<Invoice?> GetInvoiceForPaymentAsync(int invoiceId);
    Task AddPaymentAsync(Payment payment);
    Task<decimal> GetMonthlyRevenueAsync(string monthYear, int? buildingId = null);
    Task<List<DashboardRevenueTargetRecordDto>> GetRevenueTargetsAsync(int? buildingId = null);
    Task SaveChangesAsync();
}
