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

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public User Owner { get; set; } = null!;

    public Package Package { get; set; } = null!;

    public ICollection<SubscriptionPayment> Payments { get; set; } = new List<SubscriptionPayment>();
}
