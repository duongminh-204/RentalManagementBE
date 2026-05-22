using Backend.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace Backend.Repositories.Interfaces;

public interface IExcelImportRepository
{
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<Role?> GetRoleByNameAsync(string roleName, CancellationToken cancellationToken = default);
    Task AddRoleAsync(Role role, CancellationToken cancellationToken = default);
    Task<User?> GetUserByRoleIdAsync(int roleId, CancellationToken cancellationToken = default);
    Task AddUserAsync(User user, CancellationToken cancellationToken = default);
    Task<Building?> GetBuildingByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task AddBuildingAsync(Building building, CancellationToken cancellationToken = default);

    Task<Dictionary<string, Room>> GetRoomsByBuildingAsync(int buildingId, Func<string?, string> normalizeKey, CancellationToken cancellationToken = default);
    Task AddRoomAsync(Room room, CancellationToken cancellationToken = default);

    Task<List<Tenant>> GetTenantsWithContractsAsync(CancellationToken cancellationToken = default);
    Task AddTenantAsync(Tenant tenant, CancellationToken cancellationToken = default);

    Task<Contract?> GetActiveContractAsync(int roomId, int tenantId, CancellationToken cancellationToken = default);
    Task AddContractAsync(Contract contract, CancellationToken cancellationToken = default);

    Task<List<Invoice>> GetInvoicesWithPaymentsAsync(IEnumerable<int> roomIds, CancellationToken cancellationToken = default);
    Task AddInvoiceAsync(Invoice invoice, CancellationToken cancellationToken = default);
    void RemovePayments(IEnumerable<Payment> payments);
}
