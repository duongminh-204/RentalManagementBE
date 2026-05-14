namespace Backend.Entities;

public class Payment
{
    public int PaymentId { get; set; }

    public int InvoiceId { get; set; }

    public decimal Amount { get; set; }

    public string? PaymentMethod { get; set; }

    public string? TransactionCode { get; set; }

    public DateTime PaymentDate { get; set; } = DateTime.Now;

    public string Status { get; set; } = "Success";

    public string? Note { get; set; }

    public Invoice Invoice { get; set; }
}