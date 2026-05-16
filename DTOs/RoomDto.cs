namespace Backend.DTOs.Rooms
{
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
        public List<RoomUserDto> Users { get; set; } = new();
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
    }

    public class RoomUserDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Avatar { get; set; }
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
    }

    public class UpdateRoomDto : CreateRoomDto { }

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
}