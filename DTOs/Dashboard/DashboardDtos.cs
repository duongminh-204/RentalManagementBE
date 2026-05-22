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
    public int? TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public decimal Amount { get; set; }
    public List<DashboardDebtMonthDto> DebtMonths { get; set; } = new();
}

public class DashboardDebtMonthDto
{
    public string MonthYear { get; set; } = string.Empty;
    public decimal OutstandingAmount { get; set; }
    public string? Status { get; set; }
    public DateTime? DueDate { get; set; }
}

public class DashboardRoomStatusRecordDto
{
    public string? Status { get; set; }
}

public class DashboardRevenueTargetRecordDto
{
    public decimal Price { get; set; }
    public string? Status { get; set; }
}

public class DashboardDebtRecordDto
{
    public int RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public int? TenantId { get; set; }
    public string? TenantName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string MonthYear { get; set; } = string.Empty;
    public string? Status { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal OutstandingAmount { get; set; }
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
