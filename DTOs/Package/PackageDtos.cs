namespace Backend.DTOs.Package;

public class PublicPackageDto
{
    public int PackageId { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public string RoomRange { get; set; } = string.Empty;
    public string TargetAudience { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int MaxRooms { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool Recommended { get; set; }
    public List<string> Features { get; set; } = [];
}

public class OwnerSubscriptionDto
{
    public int? SubscriptionId { get; set; }
    public int? PackageId { get; set; }
    public string? PackageName { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<string> Features { get; set; } = [];
}

public class RequestSubscriptionDto
{
    public int PackageId { get; set; }
}
