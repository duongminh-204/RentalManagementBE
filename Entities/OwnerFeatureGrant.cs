using System.ComponentModel.DataAnnotations;

namespace Backend.Entities;

public class OwnerFeatureGrant
{
    public int OwnerFeatureGrantId { get; set; }

    public int OwnerUserId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Feature { get; set; } = string.Empty;

    public DateTime? ExpiresAt { get; set; }

    [MaxLength(300)]
    public string? Note { get; set; }

    public int? GrantedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public User Owner { get; set; } = null!;

    public User? GrantedBy { get; set; }
}
