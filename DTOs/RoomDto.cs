namespace Backend.DTOs.Rooms
{
    public class RoomDto
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public decimal RentalPrice { get; set; }
        public decimal ElectricPrice { get; set; }
        public decimal WaterPrice { get; set; }
        public decimal InternetPrice { get; set; }
        public string? AdditionalServices { get; set; }
        public string? Description { get; set; }
        public string Status { get; set; } = "vacant";
        public int BuildingId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateRoomDto
    {
        public string RoomNumber { get; set; } = string.Empty;
        public decimal RentalPrice { get; set; }
        public decimal ElectricPrice { get; set; }
        public decimal WaterPrice { get; set; }
        public decimal InternetPrice { get; set; }
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