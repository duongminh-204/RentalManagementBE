namespace Backend.DTOs.Vehicles;

public class VehicleDto
{
    public int Id { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Brand { get; set; }
    public string? Color { get; set; }
    public string? ImageUrl { get; set; }
    public decimal ParkingFee { get; set; }
    public string Status { get; set; } = "active";
    public string? Notes { get; set; }
    public DateTime? RegistrationDate { get; set; }
    public int? TenantId { get; set; }
    public string? TenantName { get; set; }
    public int? RoomId { get; set; }
    public string? RoomNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateVehicleDto
{
    public string LicensePlate { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Brand { get; set; }
    public string? Color { get; set; }
    public decimal ParkingFee { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public DateTime? RegistrationDate { get; set; }
    public int? TenantId { get; set; }
    public int? RoomId { get; set; }
}

public class UpdateVehicleDto : CreateVehicleDto;

public class UploadVehicleImageResponseDto
{
    public string ImageUrl { get; set; } = string.Empty;
}

public class ParkingFeeSummaryDto
{
    public decimal TotalMonthlyFee { get; set; }
    public int ActiveVehicleCount { get; set; }
}
