namespace Backend.Entities;

public class Contract
{
    public int ContractId { get; set; }

    public int RoomId { get; set; }

    public int UserId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public decimal Deposit { get; set; }

    public string? ContractFile { get; set; }

    public string Status { get; set; } = "Active";

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    
    public Room? Room { get; set; }

    public User? User { get; set; }
}