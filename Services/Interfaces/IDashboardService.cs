using Backend.DTOs.Dashboard;

namespace Backend.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardStatsDto> GetDashboardStatsAsync(int month, int year, int? buildingId = null);
    Task<DashboardRoomStatsDto> GetRoomStatsAsync(int? buildingId = null);
    Task<DashboardDebtInfoDto> GetDebtInfoAsync(int? buildingId = null);
    Task<DashboardDebtPaymentResultDto> RecordDebtPaymentAsync(int invoiceId, DashboardDebtPaymentRequestDto request);
    Task<DashboardDebtPaymentResultDto> RestoreDebtItemAsync(int invoiceId, string itemKey);
    Task<DashboardRevenueDto> GetRevenueAsync(int month, int year, int? buildingId = null);
    Task<(byte[] Content, string FileName)> ExportDashboardExcelAsync(int month, int year, int? buildingId = null);
}
