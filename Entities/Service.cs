namespace Backend.Entities;

public class Service
{
    public int ServiceId { get; set; }

    public string ServiceName { get; set; }

    public decimal UnitPrice { get; set; }

    public string? Unit { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<RoomService> RoomServices { get; set; } = new List<RoomService>();
}