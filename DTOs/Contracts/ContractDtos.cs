namespace Backend.DTOs.Contracts;

public class ContractDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int RoomId { get; set; }
    public int? ParentContractId { get; set; }
    public string? TenantName { get; set; }
    public string? RoomName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal RentPrice { get; set; }
    public decimal Deposit { get; set; }
    public string PaymentCycle { get; set; } = "Monthly";
    public string DepositStatus { get; set; } = "Holding";
    public decimal DepositRefundAmount { get; set; }
    public decimal DepositDeductionAmount { get; set; }
    public string? Terms { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = "active";
    public string? FileUrl { get; set; }
    public bool IsTerminated { get; set; }
    public string? TerminationReason { get; set; }
    public DateTime? TerminatedAt { get; set; }
    public List<RenewalHistoryItemDto> RenewalHistory { get; set; } = [];
    public List<DepositHistoryItemDto> DepositHistory { get; set; } = [];
}

public class ContractDetailDto : ContractDto
{
    public TenantSummaryDto? Tenant { get; set; }
    public RoomSummaryDto? Room { get; set; }
    public List<PaymentHistoryItemDto> PaymentHistory { get; set; } = [];
}

public class TenantSummaryDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? CCCD { get; set; }
    public string? Address { get; set; }
}

public class RoomSummaryDto
{
    public int Id { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public double? Area { get; set; }
}

public class PaymentHistoryItemDto
{
    public int InvoiceId { get; set; }
    public string MonthYear { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? PaymentDate { get; set; }
    public decimal PaidAmount { get; set; }
}

public class RenewalHistoryItemDto
{
    public int? FromContractId { get; set; }
    public int? ToContractId { get; set; }
    public DateTime RenewedAt { get; set; }
    public DateTime OldEndDate { get; set; }
    public DateTime NewEndDate { get; set; }
    public decimal OldRentPrice { get; set; }
    public decimal NewRentPrice { get; set; }
    public int ExtendMonths { get; set; }
    public string? Notes { get; set; }
}

public class DepositHistoryItemDto
{
    public DateTime ChangedAt { get; set; }
    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Note { get; set; }
}

public class ContractReminderDto
{
    public int ContractId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public DateTime EndDate { get; set; }
    public int DaysRemaining { get; set; }
    public string ReminderType { get; set; } = string.Empty;
}

public class CreateContractDto
{
    public int TenantId { get; set; }
    public int RoomId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal RentPrice { get; set; }
    public decimal? Deposit { get; set; }
    public string PaymentCycle { get; set; } = "Monthly";
    public string? Terms { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = "Active";
}

public class UpdateContractDto : CreateContractDto;

public class RenewContractDto
{
    public int ExtendMonths { get; set; } = 12;
    public decimal? NewRentPrice { get; set; }
    public bool CloneContract { get; set; } = true;
    public string? Notes { get; set; }
}

public class TerminateContractDto
{
    public string Reason { get; set; } = string.Empty;
    public decimal DepositDeductionAmount { get; set; }
    public string? Notes { get; set; }
}

public class UpdateDepositDto
{
    public string DepositStatus { get; set; } = "Holding";
    public decimal? RefundAmount { get; set; }
    public decimal? DeductionAmount { get; set; }
    public string? Note { get; set; }
}

public class GenerateContractDto
{
    public string? TemplateName { get; set; }
}
