using System;

namespace Backend.Entities;

public class UtilityUsage
{
    public int UsageId { get; set; }

    public int RoomId { get; set; }

    public string MonthYear { get; set; }

    public int ElectricNumberBf { get; set; }

    public int ElectricNumberAt { get; set; }


    public int ElectricConsumed { get; private set; }

    public int WaterNumberBf { get; set; }

    public int WaterNumberAt { get; set; }

    
    public int WaterConsumed { get; private set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public Room Room { get; set; }
}