using System.ComponentModel.DataAnnotations;

namespace Backend.Entities;

public class User
{
    public int UserId { get; set; }

    public int RoleId { get; set; }

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

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public string? Avatar { get; set; }
    public string? Address { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Navigation Properties
    public Role Role { get; set; } = null!;
    public ICollection<Building> Buildings { get; set; } = new List<Building>();
    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}