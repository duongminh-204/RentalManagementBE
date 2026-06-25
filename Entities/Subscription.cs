using System.ComponentModel.DataAnnotations;

namespace Backend.Entities;

public class Subscription
{
    public int SubscriptionId { get; set; }

    public int OwnerUserId { get; set; }

    public int PackageId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Active";

    /// <summary>Mã tham chiếu chuyển khoản (vd: DK42) — dùng trong nội dung VietQR.</summary>
    [MaxLength(30)]
    public string? PaymentReference { get; set; }

    /// <summary>Số tiền cần thanh toán (null = giá đầy đủ của gói; dùng cho nâng cấp prorated).</summary>
    public decimal? PaymentAmount { get; set; }

    /// <summary>Gói đang hoạt động sẽ bị thay thế sau khi thanh toán nâng cấp thành công.</summary>
    public int? ReplacesSubscriptionId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public User Owner { get; set; } = null!;

    public Package Package { get; set; } = null!;

    public ICollection<SubscriptionPayment> Payments { get; set; } = new List<SubscriptionPayment>();
}
