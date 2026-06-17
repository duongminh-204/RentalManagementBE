namespace Backend.DTOs.Invoices;

public class InvoiceDto
{
    public int InvoiceId { get; set; }
    public int RoomId { get; set; }
    public int UserId { get; set; }
    public string? RoomName { get; set; }
    public string? TenantName { get; set; }
    public string MonthYear { get; set; } = string.Empty;
    public int ElectricConsumed { get; set; }
    public int WaterConsumed { get; set; }
    public decimal RoomFee { get; set; }
    public decimal ElectricFee { get; set; }
    public decimal WaterFee { get; set; }
    public decimal ServiceFee { get; set; }
    public decimal ParkingFee { get; set; }
    public decimal OtherFee { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountDue { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public string? QRCodeUrl { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<InvoiceDetailDto> InvoiceDetails { get; set; } = new List<InvoiceDetailDto>();
}
