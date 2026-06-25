using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Backend.Entities;

public class User : IdentityUser<int>
{
    [NotMapped]
    public int UserId
    {
        get => Id;
        set => Id = value;
    }

    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? CCCD { get; set; }

    public string? CCCDImage { get; set; }

    public string? Avatar { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(256)]
    public string? VisiblePassword { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsSuspended { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public ICollection<Building> Buildings { get; set; } = new List<Building>();

    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();

    public ICollection<SubscriptionPayment> SubscriptionPayments { get; set; } = new List<SubscriptionPayment>();

    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
