using Backend.Data;
using Backend.Entities;
using Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories;

public class LegalRepository : ILegalRepository
{
    private readonly RentalManagementDb _db;

    public LegalRepository(RentalManagementDb db)
    {
        _db = db;
    }

    public async Task<List<Room>> GetOwnerRoomsWithDetailsAsync(int ownerUserId, CancellationToken ct = default) =>
        await _db.Rooms
            .AsNoTracking()
            .Include(r => r.Building)
            .Include(r => r.RoomImages)
            .Include(r => r.Contracts)
                .ThenInclude(c => c.Tenant)
            .Where(r => r.Building.UserId == ownerUserId)
            .OrderBy(r => r.Building.BuildingName)
            .ThenBy(r => r.RoomName)
            .ToListAsync(ct);

    public async Task<List<Tenant>> GetOwnerTenantsWithDetailsAsync(int ownerUserId, CancellationToken ct = default) =>
        await _db.Tenants
            .AsNoTracking()
            .Include(t => t.Contracts)
                .ThenInclude(c => c.Room)
                    .ThenInclude(r => r!.Building)
            .Where(t => t.IsActive && t.Contracts.Any(c =>
                c.Status == "Active" &&
                c.Room != null &&
                c.Room.Building.UserId == ownerUserId))
            .OrderBy(t => t.FullName)
            .ToListAsync(ct);

    public async Task<Tenant?> GetOwnerTenantByIdAsync(int tenantId, int ownerUserId, CancellationToken ct = default) =>
        await _db.Tenants
            .AsNoTracking()
            .Include(t => t.Contracts)
                .ThenInclude(c => c.Room)
                    .ThenInclude(r => r!.Building)
            .FirstOrDefaultAsync(t =>
                t.TenantId == tenantId &&
                t.Contracts.Any(c => c.Room != null && c.Room.Building.UserId == ownerUserId),
                ct);

    public async Task<Room?> GetOwnerRoomByIdAsync(int roomId, int ownerUserId, CancellationToken ct = default) =>
        await _db.Rooms
            .AsNoTracking()
            .Include(r => r.Building)
            .Include(r => r.RoomImages)
            .Include(r => r.Contracts)
                .ThenInclude(c => c.Tenant)
            .FirstOrDefaultAsync(r => r.RoomId == roomId && r.Building.UserId == ownerUserId, ct);

    public async Task<TenantLegalProfile?> GetTenantLegalProfileAsync(int tenantId, CancellationToken ct = default) =>
        await _db.TenantLegalProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId, ct);

    public async Task<RoomLegalProfile?> GetRoomLegalProfileAsync(int roomId, CancellationToken ct = default) =>
        await _db.RoomLegalProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.RoomId == roomId, ct);

    public async Task<TenantLegalProfile> GetOrCreateTenantLegalProfileAsync(int tenantId, CancellationToken ct = default)
    {
        var profile = await _db.TenantLegalProfiles.FirstOrDefaultAsync(p => p.TenantId == tenantId, ct);
        if (profile != null) return profile;

        profile = new TenantLegalProfile { TenantId = tenantId };
        _db.TenantLegalProfiles.Add(profile);
        await _db.SaveChangesAsync(ct);
        return profile;
    }

    public async Task<RoomLegalProfile> GetOrCreateRoomLegalProfileAsync(int roomId, CancellationToken ct = default)
    {
        var profile = await _db.RoomLegalProfiles.FirstOrDefaultAsync(p => p.RoomId == roomId, ct);
        if (profile != null) return profile;

        profile = new RoomLegalProfile { RoomId = roomId };
        _db.RoomLegalProfiles.Add(profile);
        await _db.SaveChangesAsync(ct);
        return profile;
    }

    public async Task<List<BuildingLegalDocument>> GetBuildingDocumentsAsync(int? buildingId, int ownerUserId, CancellationToken ct = default)
    {
        var query = _db.BuildingLegalDocuments
            .AsNoTracking()
            .Include(d => d.Building)
            .Where(d => d.Building.UserId == ownerUserId);

        if (buildingId.HasValue)
            query = query.Where(d => d.BuildingId == buildingId.Value);

        return await query
            .OrderBy(d => d.Building.BuildingName)
            .ThenBy(d => d.Title)
            .ToListAsync(ct);
    }

    public async Task<BuildingLegalDocument?> GetBuildingDocumentByIdAsync(int documentId, int ownerUserId, CancellationToken ct = default) =>
        await _db.BuildingLegalDocuments
            .Include(d => d.Building)
            .FirstOrDefaultAsync(d => d.BuildingLegalDocumentId == documentId && d.Building.UserId == ownerUserId, ct);

    public async Task<Building?> GetOwnerBuildingAsync(int buildingId, int ownerUserId, CancellationToken ct = default) =>
        await _db.Buildings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.BuildingId == buildingId && b.UserId == ownerUserId, ct);

    public void AddBuildingDocument(BuildingLegalDocument document) => _db.BuildingLegalDocuments.Add(document);

    public void RemoveBuildingDocument(BuildingLegalDocument document) => _db.BuildingLegalDocuments.Remove(document);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
