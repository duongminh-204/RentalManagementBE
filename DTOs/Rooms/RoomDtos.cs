namespace Backend.DTOs.Rooms;

public class RoomDto
{
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public decimal RentalPrice { get; set; }
    public decimal Price { get; set; }
    public decimal ElectricPrice { get; set; }
    public decimal WaterPrice { get; set; }
    public string? AdditionalServices { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "vacant";
    public int BuildingId { get; set; }
    public double? Area { get; set; }
    public int? MaxPeople { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class RoomDetailDto : RoomDto
{
    public List<RoomImageDto> RoomImages { get; set; } = new();
    public List<RoomDeviceDto> Devices { get; set; } = new();
    public List<RoomTenantDto> Tenants { get; set; } = new();
    public List<RoomServiceItemDto> RoomServices { get; set; } = new();
}

public class RoomImageDto
{
    public int RoomImageId { get; set; }
    public int RoomId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
}

public class RoomDeviceDto
{
    public int DeviceId { get; set; }
    public int RoomId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Status { get; set; } = "Working";
    public string? Note { get; set; }
    public string? ImageUrl { get; set; }
}

public class RoomTenantDto
{
    public int ContractId { get; set; }
    public int TenantId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
}

public class CreateRoomDto
{
    public string RoomNumber { get; set; } = string.Empty;
    public decimal RentalPrice { get; set; }
    public decimal ElectricPrice { get; set; }
    public decimal WaterPrice { get; set; }
    public string? AdditionalServices { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "vacant";
    public int BuildingId { get; set; }
    public double? Area { get; set; }
    public int? MaxPeople { get; set; }
}

public class UpdateRoomDto : CreateRoomDto;

public class RoomStatusUpdateDto
{
    public string Status { get; set; } = "vacant";
}

public class RoomStatsDto
{
    public int Total { get; set; }
    public int Occupied { get; set; }
    public int Vacant { get; set; }
    public int Maintenance { get; set; }
}

public class ServiceCatalogDto
{
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    // "Monthly" (theo tháng) hoặc "Yearly" (theo năm)
    public string BillingCycle { get; set; } = "Monthly";
    public string? Unit { get; set; }
    public string? Description { get; set; }
}

public class DeviceCatalogDto
{
    public int DeviceCatalogId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Icon { get; set; }
}

public class RoomServiceItemDto
{
    public int RoomServiceId { get; set; }
    public int RoomId { get; set; }
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public string? Unit { get; set; }
    public int Quantity { get; set; }
}

public class CreateRoomImageDto
{
    public string ImageUrl { get; set; } = string.Empty;
}

public class CreateDeviceDto
{
    public string DeviceName { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public string Status { get; set; } = "Working";
    public string? Note { get; set; }
    public string? ImageUrl { get; set; }
}

public class UpdateDeviceDto : CreateDeviceDto;

public class AssignRoomServiceDto
{
    public int ServiceId { get; set; }
    public int Quantity { get; set; } = 1;
}

public class UpdateRoomServiceDto
{
    public int Quantity { get; set; } = 1;
}

public class AssignTenantDto
{
    public int TenantId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal Deposit { get; set; }
}

public class TenantPickerDto
{
    public int TenantId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
}

public class TenantAssignmentDto
{
    public int ContractId { get; set; }
    public int TenantId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = "Active";
}
