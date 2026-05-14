namespace Backend.Entities;

public class Building
{
    public int BuildingId { get; set; }

    public int UserId { get; set; }

    public string BuildingName { get; set; }

    public string Address { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public User User { get; set; }

    public ICollection<Room> Rooms { get; set; } = new List<Room>();

    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}