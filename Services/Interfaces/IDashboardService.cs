using Backend.DTOs.Dashboard;

namespace Backend.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardStatsDto> GetDashboardStatsAsync(int month, int year, int? buildingId = null);
    Task<DashboardRoomStatsDto> GetRoomStatsAsync(int? buildingId = null);
    Task<DashboardDebtInfoDto> GetDebtInfoAsync(int? buildingId = null);
    Task<DashboardRevenueDto> GetRevenueAsync(int month, int year, int? buildingId = null);
}
