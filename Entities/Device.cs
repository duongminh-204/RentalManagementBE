namespace Backend.Entities;

public class Device
{
    public int DeviceId { get; set; }

    public int RoomId { get; set; }

    public string DeviceName { get; set; }

    public int Quantity { get; set; } = 1;

    public string Status { get; set; } = "Working";

    public string? Note { get; set; }

    public Room Room { get; set; }
}