using System.ComponentModel.DataAnnotations;

namespace Backend.Entities;

public class AuditLog
{
    public int LogId { get; set; }

    public int? UserId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Entity { get; set; }

    public int? EntityId { get; set; }

    [MaxLength(45)]
    public string? IPAddress { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.Now;

    [MaxLength(500)]
    public string? Details { get; set; }

    public User? User { get; set; }
}
