using Backend.Data;
using Backend.Entities;
using Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories;

public class TenantRepository : ITenantRepository
{
    private readonly RentalManagementDb _db;

    public TenantRepository(RentalManagementDb db)
    {
        _db = db;
    }

    public async Task<List<Tenant>> ListWithContractsAndRoomsAsync(int? ownerUserId = null, CancellationToken cancellationToken = default)
    {
        var query = _db.Tenants
            .AsNoTracking()
            .Include(t => t.Contracts)
            .ThenInclude(c => c.Room)
            .ThenInclude(r => r.Building)
            .AsQueryable();

        if (ownerUserId.HasValue)
            query = query.Where(t => t.Contracts.Any(c => c.Room != null && c.Room.Building.UserId == ownerUserId.Value));

        return await query
            .OrderByDescending(t => t.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Tenant?> GetWithContractsAndRoomsByIdAsync(int tenantId, int? ownerUserId = null, CancellationToken cancellationToken = default) =>
        await _db.Tenants
            .AsNoTracking()
            .Include(t => t.Contracts)
            .ThenInclude(c => c.Room)
            .ThenInclude(r => r.Building)
            .FirstOrDefaultAsync(
                t => t.TenantId == tenantId &&
                     (!ownerUserId.HasValue || t.Contracts.Any(c => c.Room != null && c.Room.Building.UserId == ownerUserId.Value)),
                cancellationToken);

    public async Task<Tenant?> GetTrackedWithContractsByIdAsync(int tenantId, CancellationToken cancellationToken = default) =>
        await _db.Tenants
            .Include(t => t.Contracts)
            .FirstOrDefaultAsync(t => t.TenantId == tenantId, cancellationToken);

    public async Task<Tenant?> GetTrackedByIdAsync(int tenantId, CancellationToken cancellationToken = default) =>
        await _db.Tenants.FirstOrDefaultAsync(t => t.TenantId == tenantId, cancellationToken);

    public async Task<bool> IsEmailOrPhoneTakenAsync(string? email, string? phone, int? excludeTenantId, CancellationToken cancellationToken = default)
    {
        return await _db.Tenants.AnyAsync(t =>
            (!excludeTenantId.HasValue || t.TenantId != excludeTenantId.Value) &&
            ((email != null && t.Email == email) || (phone != null && t.PhoneNumber == phone)),
            cancellationToken);
    }

    public void Add(Tenant tenant) => _db.Tenants.Add(tenant);

    public async Task<Room?> GetRoomAsync(int roomId, CancellationToken cancellationToken = default) =>
        await _db.Rooms.FirstOrDefaultAsync(r => r.RoomId == roomId, cancellationToken);

    public async Task<Contract?> FindActiveContractByTenantAndRoomAsync(int tenantId, int roomId, CancellationToken cancellationToken = default) =>
        await _db.Contracts.FirstOrDefaultAsync(c =>
            c.TenantId == tenantId && c.RoomId == roomId && c.Status == "Active", cancellationToken);

    public async Task<List<Contract>> GetActiveContractsForTenantAsync(int tenantId, CancellationToken cancellationToken = default) =>
        await _db.Contracts
            .Where(c => c.TenantId == tenantId && c.Status == "Active")
            .ToListAsync(cancellationToken);

    public void AddContract(Contract contract) => _db.Contracts.Add(contract);

    public async Task<List<Contract>> GetContractHistoryForTenantAsync(int tenantId, CancellationToken cancellationToken = default) =>
        await _db.Contracts
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId)
            .Include(c => c.Room)
            .ThenInclude(r => r.Building)
            .OrderByDescending(c => c.StartDate)
            .ToListAsync(cancellationToken);

    public async Task<Contract?> GetContractByRoomAndIdAsync(int roomId, int contractId, CancellationToken cancellationToken = default) =>
        await _db.Contracts.FirstOrDefaultAsync(c => c.ContractId == contractId && c.RoomId == roomId, cancellationToken);

    public async Task<bool> RoomHasActiveContractAsync(int roomId, CancellationToken cancellationToken = default) =>
        await _db.Contracts.AnyAsync(c => c.RoomId == roomId && c.Status == "Active", cancellationToken);

    public void Remove(Tenant tenant) => _db.Tenants.Remove(tenant);

    public void RemoveContracts(IEnumerable<Contract> contracts) => _db.Contracts.RemoveRange(contracts);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
