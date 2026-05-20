using Backend.Entities;

namespace Backend.Repositories.Interfaces;

public interface IContractRepository
{
    Task<List<Contract>> ListAsync(int? roomId = null, int? tenantId = null, CancellationToken cancellationToken = default);
    Task<Contract?> GetByIdAsync(int contractId, CancellationToken cancellationToken = default);
    Task<Contract?> GetTrackedByIdAsync(int contractId, CancellationToken cancellationToken = default);
    Task<Room?> GetRoomAsync(int roomId, CancellationToken cancellationToken = default);
    Task<Tenant?> GetTenantAsync(int tenantId, CancellationToken cancellationToken = default);
    Task<Contract?> FindActiveByTenantAndRoomAsync(int tenantId, int roomId, CancellationToken cancellationToken = default);
    void Add(Contract contract);
    void Remove(Contract contract);
    Task<bool> RoomHasActiveContractAsync(int roomId, int? excludeContractId = null, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
