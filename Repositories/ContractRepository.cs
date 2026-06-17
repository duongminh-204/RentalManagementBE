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

    public async Task<List<Contract>> ListAsync(
        int? roomId = null,
        int? tenantId = null,
        string? search = null,
        string? statusFilter = null,
        string? sortBy = null,
        bool sortDesc = true,
        int? ownerUserId = null,
        CancellationToken cancellationToken = default)
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
        if (ownerUserId.HasValue)
            query = query.Where(c => c.Room != null && c.Room.Building.UserId == ownerUserId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c =>
                (c.Room != null && c.Room.RoomName.ToLower().Contains(term)) ||
                (c.Tenant != null && c.Tenant.FullName.ToLower().Contains(term)));
        }

        var today = DateTime.UtcNow.Date;
        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            query = statusFilter.Trim().ToLowerInvariant() switch
            {
                "active" => query.Where(c =>
                    c.Status == "Active" && c.StartDate.Date <= today && c.EndDate.Date >= today),
                "expiring" or "expiring_soon" => query.Where(c =>
                    c.Status == "Active" &&
                    c.EndDate.Date >= today &&
                    c.EndDate.Date <= today.AddDays(30)),
                "expired" => query.Where(c =>
                    c.Status == "Active" && c.EndDate.Date < today),
                "cancelled" => query.Where(c => c.Status == "Cancelled"),
                "terminated" => query.Where(c => c.Status == "Terminated"),
                _ => query
            };
        }

        query = (sortBy?.Trim().ToLowerInvariant()) switch
        {
            "startdate" => sortDesc
                ? query.OrderByDescending(c => c.StartDate)
                : query.OrderBy(c => c.StartDate),
            "enddate" => sortDesc
                ? query.OrderByDescending(c => c.EndDate)
                : query.OrderBy(c => c.EndDate),
            _ => sortDesc
                ? query.OrderByDescending(c => c.CreatedAt)
                : query.OrderBy(c => c.CreatedAt)
        };

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<List<Contract>> GetExpiringAsync(int days, int? ownerUserId = null, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var target = today.AddDays(days);

        var query = _db.Contracts
            .AsNoTracking()
            .Include(c => c.Room)
            .Include(c => c.Tenant)
            .Where(c =>
                c.Status == "Active" &&
                c.EndDate.Date >= today &&
                c.EndDate.Date <= target);

        if (ownerUserId.HasValue)
            query = query.Where(c => c.Room != null && c.Room.Building.UserId == ownerUserId.Value);

        return await query
            .OrderBy(c => c.EndDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Contract>> GetExpiredNotRenewedAsync(int? ownerUserId = null, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;

        var query = _db.Contracts
            .AsNoTracking()
            .Include(c => c.Room)
            .Include(c => c.Tenant)
            .Where(c =>
                c.Status == "Active" &&
                c.EndDate.Date < today);

        if (ownerUserId.HasValue)
            query = query.Where(c => c.Room != null && c.Room.Building.UserId == ownerUserId.Value);

        return await query
            .OrderBy(c => c.EndDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<Contract?> GetByIdAsync(int contractId, int? ownerUserId = null, CancellationToken cancellationToken = default) =>
        await _db.Contracts
            .AsNoTracking()
            .Include(c => c.Room)
            .Include(c => c.Tenant)
            .FirstOrDefaultAsync(
                c => c.ContractId == contractId &&
                     (!ownerUserId.HasValue || (c.Room != null && c.Room.Building.UserId == ownerUserId.Value)),
                cancellationToken);

    public async Task<Contract?> GetTrackedByIdAsync(int contractId, CancellationToken cancellationToken = default) =>
        await _db.Contracts
            .Include(c => c.Room)
            .Include(c => c.Tenant)
            .FirstOrDefaultAsync(c => c.ContractId == contractId, cancellationToken);

    public async Task<Room?> GetRoomAsync(int roomId, CancellationToken cancellationToken = default) =>
        await _db.Rooms.FirstOrDefaultAsync(r => r.RoomId == roomId, cancellationToken);

    public async Task<Tenant?> GetTenantAsync(int tenantId, CancellationToken cancellationToken = default) =>
        await _db.Tenants.FirstOrDefaultAsync(t => t.TenantId == tenantId, cancellationToken);

    public async Task<Contract?> FindActiveByTenantAndRoomAsync(int tenantId, int roomId, CancellationToken cancellationToken = default) =>
        await _db.Contracts.FirstOrDefaultAsync(
            c => c.TenantId == tenantId && c.RoomId == roomId && c.Status == "Active",
            cancellationToken);

    public async Task<List<Invoice>> GetInvoicesWithPaymentsByRoomAsync(int roomId, CancellationToken cancellationToken = default) =>
        await _db.Invoices
            .AsNoTracking()
            .Include(i => i.Payments)
            .Where(i => i.RoomId == roomId)
            .OrderByDescending(i => i.MonthYear)
            .ToListAsync(cancellationToken);

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
