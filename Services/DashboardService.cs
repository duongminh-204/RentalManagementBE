using Backend.Data;
using Backend.DTOs.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class DashboardService : Interfaces.IDashboardService
{
    private readonly RentalManagementDb _context;

    public DashboardService(RentalManagementDb context)
    {
        _context = context;
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
        var roomQuery = _context.Rooms.AsNoTracking().AsQueryable();
        if (buildingId.HasValue)
        {
            roomQuery = roomQuery.Where(room => room.BuildingId == buildingId.Value);
        }

        var rooms = await roomQuery
            .Select(room => new { room.Status })
            .ToListAsync();

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
        var invoiceQuery = BuildInvoiceQuery(buildingId);

        var debtRows = await invoiceQuery
            .Select(invoice => new
            {
                invoice.RoomId,
                RoomName = invoice.Room.RoomName,
                TenantName = invoice.Room.Contracts
                    .Where(contract => contract.Status.ToLower() == "active" && contract.Tenant != null)
                    .OrderByDescending(contract => contract.StartDate)
                    .Select(contract => contract.Tenant!.FullName)
                    .FirstOrDefault(),
                OutstandingAmount = invoice.TotalAmount -
                    invoice.Payments
                        .Where(payment => payment.Status == null || payment.Status.ToLower() == "success")
                        .Select(payment => (decimal?)payment.Amount)
                        .Sum()!.GetValueOrDefault()
            })
            .ToListAsync();

        var groupedDebts = debtRows
            .Where(row => row.OutstandingAmount > 0)
            .GroupBy(row => new { row.RoomId, row.RoomName, row.TenantName })
            .Select(group => new DashboardDebtorDto
            {
                Name = string.IsNullOrWhiteSpace(group.Key.TenantName) ? $"Phòng {group.Key.RoomName}" : group.Key.TenantName!,
                Room = group.Key.RoomName,
                Amount = group.Sum(item => item.OutstandingAmount),
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
        var invoiceQuery = BuildInvoiceQuery(buildingId);

        var monthlyRevenue = await invoiceQuery
            .Where(invoice => invoice.MonthYear == monthYear)
            .SumAsync(invoice => (decimal?)invoice.TotalAmount) ?? 0m;

        var roomQuery = _context.Rooms.AsNoTracking().AsQueryable();
        if (buildingId.HasValue)
        {
            roomQuery = roomQuery.Where(room => room.BuildingId == buildingId.Value);
        }

        var roomValues = await roomQuery
            .Select(room => new { room.Price, room.Status })
            .ToListAsync();

        var targetRevenue = roomValues
            .Where(room => NormalizeRoomStatus(room.Status) != "maintenance")
            .Sum(room => room.Price);

        return new DashboardRevenueDto
        {
            MonthlyRevenue = monthlyRevenue,
            TargetRevenue = targetRevenue,
        };
    }

    private IQueryable<Entities.Invoice> BuildInvoiceQuery(int? buildingId)
    {
        var query = _context.Invoices
            .AsNoTracking()
            .Include(invoice => invoice.Payments)
            .Include(invoice => invoice.Room)
                .ThenInclude(room => room.Contracts)
                    .ThenInclude(contract => contract.Tenant)
            .AsQueryable();

        if (buildingId.HasValue)
        {
            query = query.Where(invoice => invoice.Room.BuildingId == buildingId.Value);
        }

        return query;
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
