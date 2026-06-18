namespace Backend.DTOs.Rooms;

public class RoomDecorStyleDto
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
}

public class RoomDecorStatusDto
{
    public bool IsAvailable { get; set; }
    public string? Message { get; set; }
    public string BaseUrl { get; set; } = string.Empty;
}

public class RoomDecorResultDto
{
    public string ImageUrl { get; set; } = string.Empty;
    public string PromptId { get; set; } = string.Empty;
    public int DurationMs { get; set; }
    public bool SavedToRoom { get; set; }
    public int? RoomImageId { get; set; }
}
