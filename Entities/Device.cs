namespace Backend.Entities;

public class Device
{
    public int DeviceId { get; set; }

    public int RoomId { get; set; }

    // Liên kết tới mục trong danh mục thiết bị (DeviceCatalog).
    // Nullable để vẫn cho phép thêm thiết bị tự do không nằm trong danh mục.
    public int? DeviceCatalogId { get; set; }

    public string DeviceName { get; set; }

    public int Quantity { get; set; } = 1;

    public string Status { get; set; } = "Working";

    public string? Note { get; set; }

    public string? ImageUrl { get; set; }

    public Room Room { get; set; }

    public DeviceCatalog? DeviceCatalog { get; set; }
}