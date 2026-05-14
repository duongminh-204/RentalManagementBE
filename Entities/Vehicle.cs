namespace Backend.Entities;

public class Vehicle
{
    public int VehicleId { get; set; }

    public int UserId { get; set; }

    public int RoomId { get; set; }

    public string? VehicleType { get; set; }

    public string? Brand { get; set; }

    public string? Color { get; set; }

    public string LicensePlateNumber { get; set; }

    public string? VehicleImage { get; set; }

    public decimal ParkingFee { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public User User { get; set; }

    public Room Room { get; set; }
}