using Backend.Data;
using Backend.Entities;
using Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories;

public class ContractRepository : IContractRepository
{
    private readonly RentalManagementDb _db;

    public ContractRepository(RentalManagementDb db)
    {
        _db = db;
    }

    public async Task<List<Contract>> ListAsync(int? roomId = null, int? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _db.Contracts
            .AsNoTracking()
            .Include(c => c.Room)
            .Include(c => c.Tenant)
            .AsQueryable();

        if (roomId.HasValue)
            query = query.Where(c => c.RoomId == roomId.Value);
        if (tenantId.HasValue)
            query = query.Where(c => c.TenantId == tenantId.Value);

        return await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Contract?> GetByIdAsync(int contractId, CancellationToken cancellationToken = default) =>
        await _db.Contracts
            .AsNoTracking()
            .Include(c => c.Room)
            .Include(c => c.Tenant)
            .FirstOrDefaultAsync(c => c.ContractId == contractId, cancellationToken);

    public async Task<Contract?> GetTrackedByIdAsync(int contractId, CancellationToken cancellationToken = default) =>
        await _db.Contracts
            .Include(c => c.Room)
            .FirstOrDefaultAsync(c => c.ContractId == contractId, cancellationToken);

    public async Task<Room?> GetRoomAsync(int roomId, CancellationToken cancellationToken = default) =>
        await _db.Rooms.FirstOrDefaultAsync(r => r.RoomId == roomId, cancellationToken);

    public async Task<Tenant?> GetTenantAsync(int tenantId, CancellationToken cancellationToken = default) =>
        await _db.Tenants.FirstOrDefaultAsync(t => t.TenantId == tenantId, cancellationToken);

    public async Task<Contract?> FindActiveByTenantAndRoomAsync(int tenantId, int roomId, CancellationToken cancellationToken = default) =>
        await _db.Contracts.FirstOrDefaultAsync(
            c => c.TenantId == tenantId && c.RoomId == roomId && c.Status == "Active",
            cancellationToken);

    public void Add(Contract contract) => _db.Contracts.Add(contract);

    public void Remove(Contract contract) => _db.Contracts.Remove(contract);

    public async Task<bool> RoomHasActiveContractAsync(int roomId, int? excludeContractId = null, CancellationToken cancellationToken = default) =>
        await _db.Contracts.AnyAsync(
            c => c.RoomId == roomId &&
                 c.Status == "Active" &&
                 (!excludeContractId.HasValue || c.ContractId != excludeContractId.Value),
            cancellationToken);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
