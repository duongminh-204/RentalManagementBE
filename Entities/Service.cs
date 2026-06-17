namespace Backend.Entities;

public class Service
{
    public int ServiceId { get; set; }

    public string ServiceName { get; set; }

    public decimal UnitPrice { get; set; }

    // Chu kỳ tính giá dịch vụ: "Monthly" (theo tháng) hoặc "Yearly" (theo năm).
    public string BillingCycle { get; set; } = "Monthly";

    public string? Unit { get; set; }

    public string? Icon { get; set; }

    public ICollection<RoomService> RoomServices { get; set; } = new List<RoomService>();
}