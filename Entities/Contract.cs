namespace Backend.Entities;

public class Contract
{
    public int ContractId { get; set; }

    public int RoomId { get; set; }

    public int TenantId { get; set; }

    public int? ParentContractId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public decimal RentPrice { get; set; }

    public decimal Deposit { get; set; }

    public string PaymentCycle { get; set; } = "Monthly";

    public string DepositStatus { get; set; } = "Holding";

    public decimal DepositRefundAmount { get; set; }

    public decimal DepositDeductionAmount { get; set; }

    public string? ContractFile { get; set; }

    public string Status { get; set; } = "Active";

    public string? Note { get; set; }

    public string? TerminationReason { get; set; }

    public DateTime? TerminatedAt { get; set; }

    public string? RenewalHistory { get; set; }

    public string? DepositHistory { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public Room? Room { get; set; }

    public Tenant? Tenant { get; set; }

    public Contract? ParentContract { get; set; }
}
