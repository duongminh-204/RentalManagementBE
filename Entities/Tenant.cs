using System.ComponentModel.DataAnnotations;

namespace Backend.Entities;

public class Tenant
{
    public int TenantId { get; set; }

    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? CCCD { get; set; }

    public string? CCCDImage { get; set; }

    public string? Avatar { get; set; }

    public DateTime? DateOfBirth { get; set; }

    [MaxLength(20)]
    public string? Gender { get; set; }

    [MaxLength(100)]
    public string? Occupation { get; set; }

    [MaxLength(200)]
    public string? Workplace { get; set; }

    public string? Address { get; set; }

    public DateTime? MoveInDate { get; set; }

    public DateTime? MoveOutDate { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();

    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}