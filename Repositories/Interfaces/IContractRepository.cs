using Backend.Entities;

namespace Backend.Repositories.Interfaces;

public interface IContractRepository
{
    Task<List<Contract>> ListAsync(
        int? roomId = null,
        int? tenantId = null,
        string? search = null,
        string? statusFilter = null,
        string? sortBy = null,
        bool sortDesc = true,
        int? ownerUserId = null,
        CancellationToken cancellationToken = default);

    Task<List<Contract>> GetExpiringAsync(int days, int? ownerUserId = null, CancellationToken cancellationToken = default);

    Task<List<Contract>> GetExpiredNotRenewedAsync(int? ownerUserId = null, CancellationToken cancellationToken = default);

    Task<Contract?> GetByIdAsync(int contractId, int? ownerUserId = null, CancellationToken cancellationToken = default);

    Task<Contract?> GetTrackedByIdAsync(int contractId, CancellationToken cancellationToken = default);

    Task<Room?> GetRoomAsync(int roomId, CancellationToken cancellationToken = default);

    Task<Tenant?> GetTenantAsync(int tenantId, CancellationToken cancellationToken = default);

    Task<Contract?> FindActiveByTenantAndRoomAsync(int tenantId, int roomId, CancellationToken cancellationToken = default);

    Task<List<Invoice>> GetInvoicesWithPaymentsByRoomAsync(int roomId, CancellationToken cancellationToken = default);

    void Add(Contract contract);

    void Remove(Contract contract);

    Task<bool> RoomHasActiveContractAsync(int roomId, int? excludeContractId = null, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
