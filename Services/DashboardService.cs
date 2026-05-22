using Backend.DTOs.Dashboard;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using ClosedXML.Excel;

namespace Backend.Services;

public class DashboardService : IDashboardService
{
    private readonly IDashboardRepository _dashboardRepository;

    public DashboardService(IDashboardRepository dashboardRepository)
    {
        _dashboardRepository = dashboardRepository;
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync(int month, int year, int? buildingId = null)
    {
        var roomStats = await GetRoomStatsAsync(buildingId);
        var debtInfo = await GetDebtInfoAsync(buildingId);
        var revenue = await GetRevenueAsync(month, year, buildingId);

        return new DashboardStatsDto
        {
            TotalRooms = roomStats.TotalRooms,
            OccupiedRooms = roomStats.OccupiedRooms,
            EmptyRooms = roomStats.EmptyRooms,
            MonthlyRevenue = revenue.MonthlyRevenue,
            UnpaidTenantsCount = debtInfo.UnpaidTenantsCount,
            TotalDebt = debtInfo.TotalDebt,
        };
    }

    public async Task<DashboardRoomStatsDto> GetRoomStatsAsync(int? buildingId = null)
    {
        var rooms = await _dashboardRepository.GetRoomStatusesAsync(buildingId);

        return new DashboardRoomStatsDto
        {
            TotalRooms = rooms.Count,
            OccupiedRooms = rooms.Count(room => NormalizeRoomStatus(room.Status) == "occupied"),
            EmptyRooms = rooms.Count(room => NormalizeRoomStatus(room.Status) == "vacant"),
            MaintenanceRooms = rooms.Count(room => NormalizeRoomStatus(room.Status) == "maintenance"),
        };
    }

    public async Task<DashboardDebtInfoDto> GetDebtInfoAsync(int? buildingId = null)
    {
        var debtRows = await _dashboardRepository.GetDebtRecordsAsync(buildingId);

        var groupedDebts = debtRows
            .Where(row => row.OutstandingAmount > 0)
            .GroupBy(row => new
            {
                row.RoomId,
                row.RoomName,
                row.TenantId,
                row.TenantName,
                row.PhoneNumber,
                row.Email,
                row.Address,
            })
            .Select(group => new DashboardDebtorDto
            {
                TenantId = group.Key.TenantId,
                Name = string.IsNullOrWhiteSpace(group.Key.TenantName) ? $"Phòng {group.Key.RoomName}" : group.Key.TenantName!,
                Room = group.Key.RoomName,
                PhoneNumber = group.Key.PhoneNumber,
                Email = group.Key.Email,
                Address = group.Key.Address,
                Amount = group.Sum(item => item.OutstandingAmount),
                DebtMonths = group
                    .OrderBy(item => item.MonthYear)
                    .Select(item => new DashboardDebtMonthDto
                    {
                        MonthYear = item.MonthYear,
                        OutstandingAmount = item.OutstandingAmount,
                        Status = item.Status,
                        DueDate = item.DueDate,
                    })
                    .ToList(),
            })
            .OrderByDescending(item => item.Amount)
            .ToList();

        return new DashboardDebtInfoDto
        {
            UnpaidTenantsCount = groupedDebts.Count,
            TotalDebt = groupedDebts.Sum(item => item.Amount),
            TopDebtors = groupedDebts.Take(5).ToList(),
        };
    }

    public async Task<DashboardRevenueDto> GetRevenueAsync(int month, int year, int? buildingId = null)
    {
        var monthYear = FormatMonthYear(month, year);
        var monthlyRevenue = await _dashboardRepository.GetMonthlyRevenueAsync(monthYear, buildingId);
        var roomValues = await _dashboardRepository.GetRevenueTargetsAsync(buildingId);

        var targetRevenue = roomValues
            .Where(room => NormalizeRoomStatus(room.Status) != "maintenance")
            .Sum(room => room.Price);

        return new DashboardRevenueDto
        {
            MonthlyRevenue = monthlyRevenue,
            TargetRevenue = targetRevenue,
        };
    }

    public async Task<(byte[] Content, string FileName)> ExportDashboardExcelAsync(int month, int year, int? buildingId = null)
    {
        var roomStats = await GetRoomStatsAsync(buildingId);
        var debtInfo = await GetDebtInfoAsync(buildingId);
        var revenue = await GetRevenueAsync(month, year, buildingId);

        using var workbook = new XLWorkbook();
        var overviewSheet = workbook.Worksheets.Add("Tong quan");
        var roomSheet = workbook.Worksheets.Add("Tinh trang phong");
        var debtSheet = workbook.Worksheets.Add("Can thu");

        overviewSheet.Cell(1, 1).Value = "Bao cao dashboard nha tro";
        overviewSheet.Cell(2, 1).Value = "Ky bao cao";
        overviewSheet.Cell(2, 2).Value = $"{year:D4}-{month:D2}";
        overviewSheet.Cell(4, 1).Value = "Chi so";
        overviewSheet.Cell(4, 2).Value = "Gia tri";

        overviewSheet.Cell(5, 1).Value = "Tong so phong";
        overviewSheet.Cell(5, 2).Value = roomStats.TotalRooms;
        overviewSheet.Cell(6, 1).Value = "Phong dang thue";
        overviewSheet.Cell(6, 2).Value = roomStats.OccupiedRooms;
        overviewSheet.Cell(7, 1).Value = "Phong trong";
        overviewSheet.Cell(7, 2).Value = roomStats.EmptyRooms;
        overviewSheet.Cell(8, 1).Value = "Phong bao tri";
        overviewSheet.Cell(8, 2).Value = roomStats.MaintenanceRooms;
        overviewSheet.Cell(9, 1).Value = "Tong hoa don thang nay";
        overviewSheet.Cell(9, 2).Value = revenue.MonthlyRevenue;
        overviewSheet.Cell(10, 1).Value = "Can thu thang nay";
        overviewSheet.Cell(10, 2).Value = debtInfo.TotalDebt;
        overviewSheet.Cell(11, 1).Value = "Da thu";
        overviewSheet.Cell(11, 2).Value = Math.Max(revenue.MonthlyRevenue - Math.Min(debtInfo.TotalDebt, revenue.MonthlyRevenue), 0);
        overviewSheet.Cell(12, 1).Value = "Khach chua thanh toan";
        overviewSheet.Cell(12, 2).Value = debtInfo.UnpaidTenantsCount;

        roomSheet.Cell(1, 1).Value = "Tong so phong";
        roomSheet.Cell(1, 2).Value = "Dang thue";
        roomSheet.Cell(1, 3).Value = "Phong trong";
        roomSheet.Cell(1, 4).Value = "Bao tri";
        roomSheet.Cell(2, 1).Value = roomStats.TotalRooms;
        roomSheet.Cell(2, 2).Value = roomStats.OccupiedRooms;
        roomSheet.Cell(2, 3).Value = roomStats.EmptyRooms;
        roomSheet.Cell(2, 4).Value = roomStats.MaintenanceRooms;

        debtSheet.Cell(1, 1).Value = "Ten khach / phong";
        debtSheet.Cell(1, 2).Value = "Phong";
        debtSheet.Cell(1, 3).Value = "So tien can thu";

        if (debtInfo.TopDebtors.Count == 0)
        {
            debtSheet.Cell(2, 1).Value = "Khong co du lieu can thu noi bat";
        }
        else
        {
            for (var index = 0; index < debtInfo.TopDebtors.Count; index++)
            {
                var debtor = debtInfo.TopDebtors[index];
                debtSheet.Cell(index + 2, 1).Value = debtor.Name;
                debtSheet.Cell(index + 2, 2).Value = debtor.Room;
                debtSheet.Cell(index + 2, 3).Value = debtor.Amount;
            }
        }

        overviewSheet.Cell(9, 2).Style.NumberFormat.Format = "#,##0";
        overviewSheet.Cell(10, 2).Style.NumberFormat.Format = "#,##0";
        overviewSheet.Cell(11, 2).Style.NumberFormat.Format = "#,##0";
        debtSheet.Column(3).Style.NumberFormat.Format = "#,##0";

        StyleWorksheet(overviewSheet);
        StyleWorksheet(roomSheet);
        StyleWorksheet(debtSheet);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return (
            stream.ToArray(),
            $"dashboard-{year:D4}-{month:D2}.xlsx");
    }

    private static void StyleWorksheet(IXLWorksheet worksheet)
    {
        var usedRange = worksheet.RangeUsed();
        if (usedRange is null)
        {
            return;
        }

        usedRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        usedRange.Style.Font.FontName = "Segoe UI";

        var headerRow = worksheet.Row(1);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#EEF1FF");

        if (worksheet.Name == "Tong quan")
        {
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;
            worksheet.Range(4, 1, 4, 2).Style.Font.Bold = true;
            worksheet.Range(4, 1, 4, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#F3F4F8");
        }

        worksheet.Columns().AdjustToContents();
    }

    private static string FormatMonthYear(int month, int year) => $"{year:D4}-{month:D2}";

    private static string NormalizeRoomStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return "vacant";
        }

        return status.Trim().ToLower() switch
        {
            "available" => "vacant",
            "empty" => "vacant",
            "vacant" => "vacant",
            "occupied" => "occupied",
            "rented" => "occupied",
            "maintenance" => "maintenance",
            _ => status.Trim().ToLower(),
        };
    }
}
