namespace Backend.DTOs.Invoices;

public class InvoiceDetailDto
{
    public int InvoiceDetailId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
}
