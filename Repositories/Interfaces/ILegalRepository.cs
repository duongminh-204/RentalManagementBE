using Backend.Entities;

namespace Backend.Repositories.Interfaces;

public interface ILegalRepository
{
    Task<List<Room>> GetOwnerRoomsWithDetailsAsync(int ownerUserId, CancellationToken ct = default);
    Task<List<Tenant>> GetOwnerTenantsWithDetailsAsync(int ownerUserId, CancellationToken ct = default);
    Task<Tenant?> GetOwnerTenantByIdAsync(int tenantId, int ownerUserId, CancellationToken ct = default);
    Task<Room?> GetOwnerRoomByIdAsync(int roomId, int ownerUserId, CancellationToken ct = default);
    Task<TenantLegalProfile?> GetTenantLegalProfileAsync(int tenantId, CancellationToken ct = default);
    Task<RoomLegalProfile?> GetRoomLegalProfileAsync(int roomId, CancellationToken ct = default);
    Task<TenantLegalProfile> GetOrCreateTenantLegalProfileAsync(int tenantId, CancellationToken ct = default);
    Task<RoomLegalProfile> GetOrCreateRoomLegalProfileAsync(int roomId, CancellationToken ct = default);
    Task<List<BuildingLegalDocument>> GetBuildingDocumentsAsync(int? buildingId, int ownerUserId, CancellationToken ct = default);
    Task<BuildingLegalDocument?> GetBuildingDocumentByIdAsync(int documentId, int ownerUserId, CancellationToken ct = default);
    Task<Building?> GetOwnerBuildingAsync(int buildingId, int ownerUserId, CancellationToken ct = default);
    void AddBuildingDocument(BuildingLegalDocument document);
    void RemoveBuildingDocument(BuildingLegalDocument document);
    Task SaveChangesAsync(CancellationToken ct = default);
}
