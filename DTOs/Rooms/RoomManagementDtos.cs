namespace Backend.DTOs.Rooms;

public class ServiceCatalogDto
{
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public string? Unit { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
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
}

public class UpdateDeviceDto : CreateDeviceDto { }

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
    public int UserId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal Deposit { get; set; }
}

public class TenantPickerDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
}

public class TenantAssignmentDto
{
    public int ContractId { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = "Active";
}
