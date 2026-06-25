namespace Backend.DTOs.Legal;

public class LegalChecklistItemDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public string? FileUrl { get; set; }
    public string? Note { get; set; }
    public string? Status { get; set; }
}

public class LegalDashboardDto
{
    public int TotalRooms { get; set; }
    public int OccupiedRooms { get; set; }
    public int RoomsComplete { get; set; }
    public int RoomsIncomplete { get; set; }
    public int ExpiringContracts { get; set; }
    public int PendingTempResidence { get; set; }
    public int ExpiringDocuments { get; set; }
    public int ActionItemsCount { get; set; }
    public int LegalScore { get; set; }
    public List<LegalAlertDto> ActionItems { get; set; } = [];
}

public class LegalAlertDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Severity { get; set; } = "warning";
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int? RoomId { get; set; }
    public int? TenantId { get; set; }
    public int? ContractId { get; set; }
    public int? BuildingId { get; set; }
    public int? DocumentId { get; set; }
    public DateTime? DueDate { get; set; }
}

public class TenantLegalSummaryDto
{
    public int TenantId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? RoomName { get; set; }
    public int? RoomId { get; set; }
    public int CompletionPercent { get; set; }
    public int CompletedCount { get; set; }
    public int TotalCount { get; set; }
    public bool TempResidencePending { get; set; }
    public List<LegalChecklistItemDto> Items { get; set; } = [];
}

public class TenantLegalDetailDto : TenantLegalSummaryDto
{
    public string? PhoneNumber { get; set; }
    public string? CCCD { get; set; }
    public string? CCCDImage { get; set; }
    public DateTime? MoveInDate { get; set; }
    public int? ActiveContractId { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyContactRelation { get; set; }
    public string? DepositReceiptFile { get; set; }
    public string? TempResidenceFile { get; set; }
    public DateTime? TempResidenceDeclaredAt { get; set; }
    public bool TempResidenceCompleted { get; set; }
}

public class UpdateTenantLegalProfileDto
{
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyContactRelation { get; set; }
    public bool? TempResidenceCompleted { get; set; }
    public DateTime? TempResidenceDeclaredAt { get; set; }
}

public class RoomLegalSummaryDto
{
    public int RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string? BuildingName { get; set; }
    public int? BuildingId { get; set; }
    public string RoomStatus { get; set; } = string.Empty;
    public string? TenantName { get; set; }
    public int? TenantId { get; set; }
    public int CompletionPercent { get; set; }
    public int CompletedCount { get; set; }
    public int TotalCount { get; set; }
    public List<LegalChecklistItemDto> Items { get; set; } = [];
}

public class RoomLegalDetailDto : RoomLegalSummaryDto
{
    public string? HandoverRecordFile { get; set; }
    public bool HandoverCompleted { get; set; }
    public string? AssetConditionNote { get; set; }
    public int? ActiveContractId { get; set; }
    public string? ContractStatus { get; set; }
    public string? DepositStatus { get; set; }
    public DateTime? ContractEndDate { get; set; }
}

public class UpdateRoomLegalProfileDto
{
    public bool? HandoverCompleted { get; set; }
    public string? AssetConditionNote { get; set; }
}

public class BuildingLegalDocumentDto
{
    public int Id { get; set; }
    public int BuildingId { get; set; }
    public string BuildingName { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? FileUrl { get; set; }
    public DateTime? IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Note { get; set; }
    public bool IsExpired { get; set; }
    public bool IsExpiringSoon { get; set; }
    public int? DaysUntilExpiry { get; set; }
}

public class CreateBuildingLegalDocumentDto
{
    public string DocumentType { get; set; } = "Other";
    public string Title { get; set; } = string.Empty;
    public DateTime? IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Note { get; set; }
}

public class UpdateBuildingLegalDocumentDto
{
    public string? DocumentType { get; set; }
    public string? Title { get; set; }
    public DateTime? IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Note { get; set; }
}

public class LegalUploadResponseDto
{
    public string FileUrl { get; set; } = string.Empty;
    public string Message { get; set; } = "Tải lên thành công.";
}

public class NotificationDto
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? Type { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SyncNotificationsResultDto
{
    public int CreatedCount { get; set; }
    public string Message { get; set; } = string.Empty;
}
