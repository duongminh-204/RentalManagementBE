using Backend.Data;
using Backend.Entities;
using Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories;

public class RoomManagementRepository : IRoomManagementRepository
{
    private readonly RentalManagementDb _db;

    public RoomManagementRepository(RentalManagementDb db)
    {
        _db = db;
    }

    public async Task<List<Service>> GetActiveServicesOrderedAsync(CancellationToken cancellationToken = default) =>
        await _db.Services
            .Where(s => s.IsActive)
            .OrderBy(s => s.ServiceName)
            .ToListAsync(cancellationToken);

    public async Task<List<Tenant>> GetActiveTenantsOrderedAsync(CancellationToken cancellationToken = default) =>
        await _db.Tenants
            .Where(t => t.IsActive)
            .OrderBy(t => t.FullName)
            .ToListAsync(cancellationToken);

    public async Task<bool> RoomExistsAsync(int roomId, CancellationToken cancellationToken = default) =>
        await _db.Rooms.AnyAsync(r => r.RoomId == roomId, cancellationToken);

    public void AddRoomImage(RoomImage image) => _db.RoomImages.Add(image);

    public async Task<RoomImage?> GetRoomImageAsync(int roomId, int imageId, CancellationToken cancellationToken = default) =>
        await _db.RoomImages.FirstOrDefaultAsync(i => i.RoomImageId == imageId && i.RoomId == roomId, cancellationToken);

    public void RemoveRoomImage(RoomImage image) => _db.RoomImages.Remove(image);

    public void AddService(Service service) => _db.Services.Add(service);

    public void RemoveService(Service service) => _db.Services.Remove(service);

    public void AddDevice(Device device) => _db.Devices.Add(device);

    public async Task<Device?> GetDeviceAsync(int roomId, int deviceId, CancellationToken cancellationToken = default) =>
        await _db.Devices.FirstOrDefaultAsync(d => d.DeviceId == deviceId && d.RoomId == roomId, cancellationToken);

    public void RemoveDevice(Device device) => _db.Devices.Remove(device);

    public async Task<Service?> GetServiceByIdAsync(int serviceId, CancellationToken cancellationToken = default) =>
        await _db.Services.FirstOrDefaultAsync(s => s.ServiceId == serviceId, cancellationToken);

    public async Task<RoomService?> FindRoomServiceAsync(int roomId, int serviceId, CancellationToken cancellationToken = default) =>
        await _db.RoomServices.FirstOrDefaultAsync(rs => rs.RoomId == roomId && rs.ServiceId == serviceId, cancellationToken);

    public void AddRoomService(RoomService roomService) => _db.RoomServices.Add(roomService);

    public async Task<RoomService?> GetRoomServiceWithServiceAsync(int roomId, int roomServiceId, CancellationToken cancellationToken = default) =>
        await _db.RoomServices
            .Include(rs => rs.Service)
            .FirstOrDefaultAsync(rs => rs.RoomServiceId == roomServiceId && rs.RoomId == roomId, cancellationToken);

    public void RemoveRoomService(RoomService roomService) => _db.RoomServices.Remove(roomService);

    public async Task<Tenant?> GetTenantByIdAsync(int tenantId, CancellationToken cancellationToken = default) =>
        await _db.Tenants.FirstOrDefaultAsync(t => t.TenantId == tenantId, cancellationToken);

    public void AddContract(Contract contract) => _db.Contracts.Add(contract);

    public async Task<Contract?> GetContractByRoomAndIdAsync(int roomId, int contractId, CancellationToken cancellationToken = default) =>
        await _db.Contracts.FirstOrDefaultAsync(c => c.ContractId == contractId && c.RoomId == roomId, cancellationToken);

    public async Task<Contract?> FindActiveContractByTenantAndRoomAsync(int tenantId, int roomId, CancellationToken cancellationToken = default) =>
        await _db.Contracts.FirstOrDefaultAsync(
            c => c.TenantId == tenantId && c.RoomId == roomId && c.Status == "Active",
            cancellationToken);

    public async Task<Room?> GetRoomTrackedAsync(int roomId, CancellationToken cancellationToken = default) =>
        await _db.Rooms.FirstOrDefaultAsync(r => r.RoomId == roomId, cancellationToken);

    public async Task<bool> RoomHasActiveContractAsync(int roomId, CancellationToken cancellationToken = default) =>
        await _db.Contracts.AnyAsync(c => c.RoomId == roomId && c.Status == "Active", cancellationToken);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
