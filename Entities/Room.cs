using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Entities;

public class Room
{
    public int RoomId { get; set; }
    public int BuildingId { get; set; }

    [Required]
    [MaxLength(50)]
    public string RoomName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Status { get; set; } = "Available";

    public decimal Price { get; set; }
    public decimal ElectricPrice { get; set; }
    public decimal WaterPrice { get; set; }
    

    public double? Area { get; set; }
    public int? MaxPeople { get; set; }
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }


    public Building Building { get; set; } = null!;

    public ICollection<RoomImage> RoomImages { get; set; } = new List<RoomImage>();
    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
    public ICollection<RoomService> RoomServices { get; set; } = new List<RoomService>();
    public ICollection<Device> Devices { get; set; } = new List<Device>();
    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    public ICollection<UtilityUsage> UtilityUsages { get; set; } = new List<UtilityUsage>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<Post> Posts { get; set; } = new List<Post>();

    [NotMapped]
    public ICollection<Tenant> Tenants =>
        Contracts
            .Where(c => c.Tenant != null)
            .Select(c => c.Tenant!)
            .GroupBy(t => t.TenantId)
            .Select(g => g.First())
            .ToList();
}
