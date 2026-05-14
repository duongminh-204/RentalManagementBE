namespace Backend.Entities;

public class Post
{
    public int PostId { get; set; }

    public int RoomId { get; set; }

    public string Title { get; set; }

    public string? Description { get; set; }

    public decimal? Price { get; set; }

    public string Status { get; set; } = "Active";

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public Room Room { get; set; }
}