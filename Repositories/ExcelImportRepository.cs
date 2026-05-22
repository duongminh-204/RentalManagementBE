using Backend.Data;
using Backend.Entities;
using Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Backend.Repositories;

public class ExcelImportRepository : IExcelImportRepository
{
    private readonly RentalManagementDb _context;

    public ExcelImportRepository(RentalManagementDb context)
    {
        _context = context;
    }

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        _context.Database.BeginTransactionAsync(cancellationToken);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    public Task<Role?> GetRoleByNameAsync(string roleName, CancellationToken cancellationToken = default) =>
        _context.Roles.FirstOrDefaultAsync(role => role.Name == roleName, cancellationToken);

    public Task AddRoleAsync(Role role, CancellationToken cancellationToken = default) =>
        _context.Roles.AddAsync(role, cancellationToken).AsTask();

    public Task<User?> GetUserByRoleIdAsync(int roleId, CancellationToken cancellationToken = default) =>
        _context.Users.FirstOrDefaultAsync(user => user.RoleId == roleId, cancellationToken);

    public Task AddUserAsync(User user, CancellationToken cancellationToken = default) =>
        _context.Users.AddAsync(user, cancellationToken).AsTask();

    public Task<Building?> GetBuildingByUserIdAsync(int userId, CancellationToken cancellationToken = default) =>
        _context.Buildings.FirstOrDefaultAsync(building => building.UserId == userId, cancellationToken);

    public Task AddBuildingAsync(Building building, CancellationToken cancellationToken = default) =>
        _context.Buildings.AddAsync(building, cancellationToken).AsTask();

    public async Task<Dictionary<string, Room>> GetRoomsByBuildingAsync(
        int buildingId,
        Func<string?, string> normalizeKey,
        CancellationToken cancellationToken = default)
    {
        return await _context.Rooms
            .Where(room => room.BuildingId == buildingId)
            .ToDictionaryAsync(room => normalizeKey(room.RoomName), cancellationToken);
    }

    public Task AddRoomAsync(Room room, CancellationToken cancellationToken = default) =>
        _context.Rooms.AddAsync(room, cancellationToken).AsTask();

    public Task<List<Tenant>> GetTenantsWithContractsAsync(CancellationToken cancellationToken = default) =>
        _context.Tenants
            .Include(tenant => tenant.Contracts)
            .ToListAsync(cancellationToken);

    public Task AddTenantAsync(Tenant tenant, CancellationToken cancellationToken = default) =>
        _context.Tenants.AddAsync(tenant, cancellationToken).AsTask();

    public Task<Contract?> GetActiveContractAsync(int roomId, int tenantId, CancellationToken cancellationToken = default) =>
        _context.Contracts.FirstOrDefaultAsync(contract =>
            contract.RoomId == roomId &&
            contract.TenantId == tenantId &&
            contract.Status.ToLower() == "active", cancellationToken);

    public Task AddContractAsync(Contract contract, CancellationToken cancellationToken = default) =>
        _context.Contracts.AddAsync(contract, cancellationToken).AsTask();

    public Task<List<Invoice>> GetInvoicesWithPaymentsAsync(IEnumerable<int> roomIds, CancellationToken cancellationToken = default)
    {
        var roomIdList = roomIds.ToList();
        return _context.Invoices
            .Include(invoice => invoice.Payments)
            .Where(invoice => roomIdList.Contains(invoice.RoomId))
            .ToListAsync(cancellationToken);
    }

    public Task AddInvoiceAsync(Invoice invoice, CancellationToken cancellationToken = default) =>
        _context.Invoices.AddAsync(invoice, cancellationToken).AsTask();

    public void RemovePayments(IEnumerable<Payment> payments) =>
        _context.Payments.RemoveRange(payments);
}
