using System.ComponentModel.DataAnnotations;

namespace Backend.Entities;

public class SubscriptionPayment
{
    public int PaymentId { get; set; }

    public int OwnerUserId { get; set; }

    public int SubscriptionId { get; set; }

    public decimal Amount { get; set; }

    [Required]
    [MaxLength(50)]
    public string PaymentMethod { get; set; } = "BankTransfer";

    public DateTime PaymentDate { get; set; } = DateTime.Now;

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Success";

    /// <summary>ID giao dịch từ ngân hàng / SePay / Casso — chống xử lý trùng.</summary>
    [MaxLength(100)]
    public string? ExternalTransactionId { get; set; }

    public User Owner { get; set; } = null!;

    public Subscription Subscription { get; set; } = null!;
}
