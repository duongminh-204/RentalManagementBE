using System.ComponentModel.DataAnnotations;

namespace Backend.Entities;

public class BuildingLegalDocument
{
    public int BuildingLegalDocumentId { get; set; }

    public int BuildingId { get; set; }

    [MaxLength(50)]
    public string DocumentType { get; set; } = "Other";

    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? FileUrl { get; set; }

    public DateTime? IssueDate { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public Building Building { get; set; } = null!;
}
