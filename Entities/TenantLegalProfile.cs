using System.ComponentModel.DataAnnotations;

namespace Backend.Entities;

public class TenantLegalProfile
{
    public int TenantLegalProfileId { get; set; }

    public int TenantId { get; set; }

    [MaxLength(100)]
    public string? EmergencyContactName { get; set; }

    [MaxLength(20)]
    public string? EmergencyContactPhone { get; set; }

    [MaxLength(50)]
    public string? EmergencyContactRelation { get; set; }

    public string? DepositReceiptFile { get; set; }

    public string? TempResidenceFile { get; set; }

    public DateTime? TempResidenceDeclaredAt { get; set; }

    public bool TempResidenceCompleted { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public Tenant Tenant { get; set; } = null!;
}
