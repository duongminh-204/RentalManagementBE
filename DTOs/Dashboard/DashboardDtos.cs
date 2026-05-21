namespace Backend.DTOs.Dashboard;

public class DashboardStatsDto
{
    public int TotalRooms { get; set; }
    public int OccupiedRooms { get; set; }
    public int EmptyRooms { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public int UnpaidTenantsCount { get; set; }
    public decimal TotalDebt { get; set; }
}

public class DashboardRoomStatsDto
{
    public int TotalRooms { get; set; }
    public int OccupiedRooms { get; set; }
    public int EmptyRooms { get; set; }
    public int MaintenanceRooms { get; set; }
}

public class DashboardDebtInfoDto
{
    public int UnpaidTenantsCount { get; set; }
    public decimal TotalDebt { get; set; }
    public List<DashboardDebtorDto> TopDebtors { get; set; } = new();
}

public class DashboardDebtorDto
{
    public string Name { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class DashboardRevenueDto
{
    public decimal MonthlyRevenue { get; set; }
    public decimal TargetRevenue { get; set; }
}

public class ExcelImportResultDto
{
    public int RoomsImported { get; set; }
    public int TenantsImported { get; set; }
    public int ContractsImported { get; set; }
    public int InvoicesImported { get; set; }
    public int PaymentsImported { get; set; }
    public List<string> Warnings { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}
