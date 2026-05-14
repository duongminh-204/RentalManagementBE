namespace Backend.Entities;

public class RoomService
{
    public int RoomServiceId { get; set; }

    public int RoomId { get; set; }

    public int ServiceId { get; set; }

    public int Quantity { get; set; } = 1;

    public Room Room { get; set; }

    public Service Service { get; set; }
}