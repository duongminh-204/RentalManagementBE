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
    public int UnpaidRoomsCount { get; set; }
    public decimal TotalDebt { get; set; }
    public List<DashboardDebtorDto> Debtors { get; set; } = new();
    public List<DashboardDebtorDto> TopDebtors { get; set; } = new();
}

public class DashboardDebtorDto
{
    public int RoomId { get; set; }
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
    public int InvoiceId { get; set; }
    public string MonthYear { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public decimal RoomFee { get; set; }
    public decimal ElectricFee { get; set; }
    public decimal WaterFee { get; set; }
    public decimal ServiceFee { get; set; }
    public decimal ParkingFee { get; set; }
    public decimal OtherFee { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? Status { get; set; }
    public DateTime? DueDate { get; set; }
    public List<DashboardDebtItemDto> DebtItems { get; set; } = new();
}

public class DashboardDebtItemDto
{
    public string ItemKey { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public bool CanRestore { get; set; }
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
    public int InvoiceId { get; set; }
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
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public decimal RoomFee { get; set; }
    public decimal ElectricFee { get; set; }
    public decimal WaterFee { get; set; }
    public decimal ServiceFee { get; set; }
    public decimal ParkingFee { get; set; }
    public decimal OtherFee { get; set; }
    public decimal DiscountAmount { get; set; }
    public List<DashboardDebtPaymentRecordDto> Payments { get; set; } = new();
}

public class DashboardDebtPaymentRecordDto
{
    public decimal Amount { get; set; }
    public string? Note { get; set; }
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

public class DashboardDebtPaymentRequestDto
{
    public decimal? Amount { get; set; }
    public string? DebtItemKey { get; set; }
    public string? PaymentMethod { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? Note { get; set; }
}

public class DashboardDebtPaymentResultDto
{
    public int InvoiceId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal PaidAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public string Message { get; set; } = string.Empty;
}
