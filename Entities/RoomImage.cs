namespace Backend.Entities;

public class RoomImage
{
    public int RoomImageId { get; set; }

    public int RoomId { get; set; }

    public string ImageUrl { get; set; }

    public Room Room { get; set; }
}