using System.ComponentModel.DataAnnotations;

namespace Backend.Entities;

public class Package
{
    public int PackageId { get; set; }

    [Required]
    [MaxLength(100)]
    public string PackageName { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int MaxRooms { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(100)]
    public string? RoomRange { get; set; }

    [MaxLength(200)]
    public string? TargetAudience { get; set; }

    public bool IsRecommended { get; set; }

    public string? FeatureLines { get; set; }

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
