namespace Backend.DTOs.Invoices;

public class CreateInvoiceFromUtilityUsageDto
{
    public int RoomId { get; set; }
    public int UserId { get; set; }
    public string MonthYear { get; set; } = string.Empty;
    public int ElectricNumberBf { get; set; }
    public int ElectricNumberAt { get; set; }
    public int WaterNumberBf { get; set; }
    public int WaterNumberAt { get; set; }
    public decimal OtherFee { get; set; } = 0m;
    public decimal DiscountAmount { get; set; } = 0m;
    public decimal? ParkingFeeOverride { get; set; }
    public string? Note { get; set; }
    public bool ForceRecreate { get; set; } = false;
}
