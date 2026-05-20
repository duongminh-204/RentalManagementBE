using Backend.Entities;

namespace Backend.Repositories.Interfaces;

public interface IRoomManagementRepository
{
    Task<List<Service>> GetActiveServicesOrderedAsync(CancellationToken cancellationToken = default);
    Task<List<Tenant>> GetActiveTenantsOrderedAsync(CancellationToken cancellationToken = default);
    Task<bool> RoomExistsAsync(int roomId, CancellationToken cancellationToken = default);
    void AddRoomImage(RoomImage image);
    Task<RoomImage?> GetRoomImageAsync(int roomId, int imageId, CancellationToken cancellationToken = default);
    void RemoveRoomImage(RoomImage image);
    void AddDevice(Device device);
    Task<Device?> GetDeviceAsync(int roomId, int deviceId, CancellationToken cancellationToken = default);
    void RemoveDevice(Device device);
    Task<Service?> GetServiceByIdAsync(int serviceId, CancellationToken cancellationToken = default);
    Task<RoomService?> FindRoomServiceAsync(int roomId, int serviceId, CancellationToken cancellationToken = default);
    void AddRoomService(RoomService roomService);
    Task<RoomService?> GetRoomServiceWithServiceAsync(int roomId, int roomServiceId, CancellationToken cancellationToken = default);
    void RemoveRoomService(RoomService roomService);
    Task<Tenant?> GetTenantByIdAsync(int tenantId, CancellationToken cancellationToken = default);
    void AddContract(Contract contract);
    Task<Contract?> GetContractByRoomAndIdAsync(int roomId, int contractId, CancellationToken cancellationToken = default);
    Task<Room?> GetRoomTrackedAsync(int roomId, CancellationToken cancellationToken = default);
    Task<bool> RoomHasActiveContractAsync(int roomId, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
