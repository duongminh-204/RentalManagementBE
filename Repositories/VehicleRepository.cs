using Backend.Data;
using Backend.Entities;
using Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories;

public class VehicleRepository : IVehicleRepository
{
    private readonly RentalManagementDb _db;

    public VehicleRepository(RentalManagementDb db)
    {
        _db = db;
    }

    public IQueryable<Vehicle> QueryWithTenantAndRoom() =>
        _db.Vehicles
            .AsNoTracking()
            .Include(v => v.Tenant)
            .Include(v => v.Room)
            .ThenInclude(r => r.Building);

    public async Task<Vehicle?> GetTrackedByIdAsync(int vehicleId, CancellationToken cancellationToken = default) =>
        await _db.Vehicles.FirstOrDefaultAsync(v => v.VehicleId == vehicleId, cancellationToken);

    public async Task<bool> LicensePlateExistsAsync(string plate, int? excludeVehicleId, CancellationToken cancellationToken = default) =>
        await _db.Vehicles.AnyAsync(v =>
            v.LicensePlateNumber == plate &&
            (!excludeVehicleId.HasValue || v.VehicleId != excludeVehicleId.Value), cancellationToken);

    public void Add(Vehicle vehicle) => _db.Vehicles.Add(vehicle);

    public void Remove(Vehicle vehicle) => _db.Vehicles.Remove(vehicle);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);

    public async Task<List<Vehicle>> ListActiveForParkingSummaryAsync(CancellationToken cancellationToken = default) =>
        await _db.Vehicles
            .AsNoTracking()
            .Where(v => v.Status == "active")
            .ToListAsync(cancellationToken);

    public async Task<bool> TenantExistsAsync(int tenantId, CancellationToken cancellationToken = default) =>
        await _db.Tenants.AnyAsync(t => t.TenantId == tenantId, cancellationToken);

    public async Task<bool> RoomExistsAsync(int roomId, CancellationToken cancellationToken = default) =>
        await _db.Rooms.AnyAsync(r => r.RoomId == roomId, cancellationToken);
}
