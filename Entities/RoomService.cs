namespace Backend.Entities;

public class RoomService
{
    public int RoomServiceId { get; set; }

    public int RoomId { get; set; }

    public int ServiceId { get; set; }

    public Room Room { get; set; }

    public Service Service { get; set; }
}