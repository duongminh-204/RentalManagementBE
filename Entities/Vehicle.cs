namespace Backend.Entities;

public class Vehicle
{
    public int VehicleId { get; set; }

    public int? UserId { get; set; }

    public int? RoomId { get; set; }

    public string? VehicleType { get; set; }

    public string? Brand { get; set; }

    public string? Color { get; set; }

    public string LicensePlateNumber { get; set; } = string.Empty;

    public string? VehicleImage { get; set; }

    public decimal ParkingFee { get; set; }

    /// <summary>active | inactive | unknown</summary>
    public string Status { get; set; } = "active";

    public string? Notes { get; set; }

    public DateTime? RegistrationDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }

    public Room? Room { get; set; }
}
