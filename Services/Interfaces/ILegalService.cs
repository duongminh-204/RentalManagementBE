using Backend.DTOs.Legal;

namespace Backend.Services.Interfaces;

public interface ILegalService
{
    Task<LegalDashboardDto> GetDashboardAsync(int ownerUserId, CancellationToken ct = default);
    Task<List<LegalAlertDto>> GetAlertsAsync(int ownerUserId, CancellationToken ct = default);
    Task<List<TenantLegalSummaryDto>> GetTenantSummariesAsync(int ownerUserId, CancellationToken ct = default);
    Task<TenantLegalDetailDto?> GetTenantDetailAsync(int tenantId, int ownerUserId, CancellationToken ct = default);
    Task<TenantLegalDetailDto> UpdateTenantProfileAsync(int tenantId, int ownerUserId, UpdateTenantLegalProfileDto dto, CancellationToken ct = default);
    Task<LegalUploadResponseDto> UploadTenantDocumentAsync(int tenantId, int ownerUserId, string docType, IFormFile file, CancellationToken ct = default);
    Task<List<RoomLegalSummaryDto>> GetRoomSummariesAsync(int ownerUserId, CancellationToken ct = default);
    Task<RoomLegalDetailDto?> GetRoomDetailAsync(int roomId, int ownerUserId, CancellationToken ct = default);
    Task<RoomLegalDetailDto> UpdateRoomProfileAsync(int roomId, int ownerUserId, UpdateRoomLegalProfileDto dto, CancellationToken ct = default);
    Task<LegalUploadResponseDto> UploadRoomHandoverAsync(int roomId, int ownerUserId, IFormFile file, CancellationToken ct = default);
    Task<List<BuildingLegalDocumentDto>> GetBuildingDocumentsAsync(int? buildingId, int ownerUserId, CancellationToken ct = default);
    Task<BuildingLegalDocumentDto> CreateBuildingDocumentAsync(int buildingId, int ownerUserId, CreateBuildingLegalDocumentDto dto, CancellationToken ct = default);
    Task<BuildingLegalDocumentDto?> UpdateBuildingDocumentAsync(int documentId, int ownerUserId, UpdateBuildingLegalDocumentDto dto, CancellationToken ct = default);
    Task<bool> DeleteBuildingDocumentAsync(int documentId, int ownerUserId, CancellationToken ct = default);
    Task<LegalUploadResponseDto> UploadBuildingDocumentFileAsync(int documentId, int ownerUserId, IFormFile file, CancellationToken ct = default);
    Task<SyncNotificationsResultDto> SyncNotificationsAsync(int ownerUserId, CancellationToken ct = default);
}
