using Backend.Entities;

namespace Backend.Repositories.Interfaces;

public interface IVehicleRepository
{
    IQueryable<Vehicle> QueryWithTenantAndRoom();
    Task<Vehicle?> GetTrackedByIdAsync(int vehicleId, CancellationToken cancellationToken = default);
    Task<bool> LicensePlateExistsAsync(string plate, int? excludeVehicleId, CancellationToken cancellationToken = default);
    void Add(Vehicle vehicle);
    void Remove(Vehicle vehicle);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<bool> TenantExistsAsync(int tenantId, CancellationToken cancellationToken = default);
    Task<bool> RoomExistsAsync(int roomId, CancellationToken cancellationToken = default);
}
