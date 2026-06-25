namespace Backend.Entities;

public class RoomLegalProfile
{
    public int RoomLegalProfileId { get; set; }

    public int RoomId { get; set; }

    public string? HandoverRecordFile { get; set; }

    public bool HandoverCompleted { get; set; }

    public string? AssetConditionNote { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public Room Room { get; set; } = null!;
}
