using Backend.Entities;

namespace Backend.Repositories.Interfaces;

public interface ITenantRepository
{
    Task<List<Tenant>> ListWithContractsAndRoomsAsync(CancellationToken cancellationToken = default);
    Task<Tenant?> GetWithContractsAndRoomsByIdAsync(int tenantId, CancellationToken cancellationToken = default);
    Task<Tenant?> GetTrackedWithContractsByIdAsync(int tenantId, CancellationToken cancellationToken = default);
    Task<Tenant?> GetTrackedByIdAsync(int tenantId, CancellationToken cancellationToken = default);
    Task<bool> IsEmailOrPhoneTakenAsync(string? email, string? phone, int? excludeTenantId, CancellationToken cancellationToken = default);
    void Add(Tenant tenant);
    Task<Room?> GetRoomAsync(int roomId, CancellationToken cancellationToken = default);
    Task<Contract?> FindActiveContractByTenantAndRoomAsync(int tenantId, int roomId, CancellationToken cancellationToken = default);
    Task<List<Contract>> GetActiveContractsForTenantAsync(int tenantId, CancellationToken cancellationToken = default);
    void AddContract(Contract contract);
    Task<List<Contract>> GetContractHistoryForTenantAsync(int tenantId, CancellationToken cancellationToken = default);
    Task<Contract?> GetContractByRoomAndIdAsync(int roomId, int contractId, CancellationToken cancellationToken = default);
    Task<bool> RoomHasActiveContractAsync(int roomId, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
