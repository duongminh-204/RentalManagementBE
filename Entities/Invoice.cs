namespace Backend.Entities;

public class Invoice
{
    public int InvoiceId { get; set; }

    public int RoomId { get; set; }

    public int UserId { get; set; }

    public string MonthYear { get; set; }

    public decimal RoomFee { get; set; }

    public decimal ElectricFee { get; set; }

    public decimal WaterFee { get; set; }

    public decimal ServiceFee { get; set; }

    public decimal ParkingFee { get; set; }

    public decimal OtherFee { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public DateTime? DueDate { get; set; }

    public string Status { get; set; } = "Unpaid";

    public DateTime? PaymentDate { get; set; }

    public string? QRCodeUrl { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public Room Room { get; set; }

    public User User { get; set; }

    public ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}