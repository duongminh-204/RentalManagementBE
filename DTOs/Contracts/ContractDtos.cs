namespace Backend.DTOs.Contracts;

public class ContractDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int RoomId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Deposit { get; set; }
    public string? Terms { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = "Active";
    public string? FileUrl { get; set; }
    public bool IsTerminated { get; set; }
}

public class CreateContractDto
{
    public string? ContractNumber { get; set; }
    public int TenantId { get; set; }
    public int RoomId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal? Deposit { get; set; }
    public string? Terms { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = "Active";
   
}

public class UpdateContractDto : CreateContractDto;

public class RenewContractDto
{
    public DateTime NewEndDate { get; set; }
    public string? Notes { get; set; }
}
