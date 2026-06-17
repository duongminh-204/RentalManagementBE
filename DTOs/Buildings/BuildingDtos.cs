namespace Backend.DTOs.Buildings;

public class BuildingDto
{
    public int BuildingId { get; set; }
    public string BuildingName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int UserId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateBuildingDto
{
    public string BuildingName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Description { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int? UserId { get; set; }
}

public class UpdateBuildingDto
{
    public string BuildingName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Description { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int? UserId { get; set; }
}
