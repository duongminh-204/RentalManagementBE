namespace Backend.Entities;

/// <summary>
/// Danh mục thiết bị dùng chung cho toàn hệ thống (được seed sẵn khi chạy Web).
/// Chủ trọ tích chọn các mục ở đây để gán cho từng phòng (lưu vào bảng nối Device).
/// </summary>
public class DeviceCatalog
{
    public int DeviceCatalogId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Icon { get; set; }

    public ICollection<Device> Devices { get; set; } = new List<Device>();
}
