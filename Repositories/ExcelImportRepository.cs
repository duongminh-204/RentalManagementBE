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

    public Task<List<Building>> ListBuildingsWithOwnerAsync(CancellationToken cancellationToken = default) =>
        _context.Buildings
            .AsNoTracking()
            .Include(building => building.User)
            .OrderBy(building => building.BuildingName)
            .ToListAsync(cancellationToken);

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

    public Task<List<Room>> ListRoomsWithDetailsAsync(int? buildingId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Rooms
            .AsNoTracking()
            .Include(room => room.Building)
            .AsQueryable();

        if (buildingId.HasValue)
        {
            query = query.Where(room => room.BuildingId == buildingId.Value);
        }

        return query
            .OrderBy(room => room.Building.BuildingName)
            .ThenBy(room => room.RoomName)
            .ToListAsync(cancellationToken);
    }

    public Task<List<Tenant>> GetTenantsWithContractsAsync(CancellationToken cancellationToken = default) =>
        _context.Tenants
            .Include(tenant => tenant.Contracts)
            .ToListAsync(cancellationToken);

    public Task AddTenantAsync(Tenant tenant, CancellationToken cancellationToken = default) =>
        _context.Tenants.AddAsync(tenant, cancellationToken).AsTask();

    public Task<List<Tenant>> ListTenantsWithVehiclesAndContractsAsync(CancellationToken cancellationToken = default) =>
        _context.Tenants
            .AsNoTracking()
            .Include(tenant => tenant.Contracts)
                .ThenInclude(contract => contract.Room)
            .Include(tenant => tenant.Vehicles)
                .ThenInclude(vehicle => vehicle.Room)
            .OrderBy(tenant => tenant.FullName)
            .ToListAsync(cancellationToken);

    public Task<Contract?> GetActiveContractAsync(int roomId, int tenantId, CancellationToken cancellationToken = default) =>
        _context.Contracts.FirstOrDefaultAsync(contract =>
            contract.RoomId == roomId &&
            contract.TenantId == tenantId &&
            contract.Status.ToLower() == "active", cancellationToken);

    public Task AddContractAsync(Contract contract, CancellationToken cancellationToken = default) =>
        _context.Contracts.AddAsync(contract, cancellationToken).AsTask();

    public Task<List<Contract>> ListContractsWithRoomAndTenantAsync(int? buildingId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Contracts
            .AsNoTracking()
            .Include(contract => contract.Room)
                .ThenInclude(room => room.Building)
            .Include(contract => contract.Tenant)
            .AsQueryable();

        if (buildingId.HasValue)
        {
            query = query.Where(contract => contract.Room != null && contract.Room.BuildingId == buildingId.Value);
        }

        return query
            .OrderBy(contract => contract.Room!.RoomName)
            .ThenBy(contract => contract.StartDate)
            .ToListAsync(cancellationToken);
    }

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

    public Task<List<Vehicle>> ListVehiclesWithRoomAndTenantAsync(int? buildingId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Vehicles
            .AsNoTracking()
            .Include(vehicle => vehicle.Room)
                .ThenInclude(room => room.Building)
            .Include(vehicle => vehicle.Tenant)
            .AsQueryable();

        if (buildingId.HasValue)
        {
            query = query.Where(vehicle => vehicle.Room != null && vehicle.Room.BuildingId == buildingId.Value);
        }

        return query
            .OrderBy(vehicle => vehicle.Room != null ? vehicle.Room.RoomName : string.Empty)
            .ThenBy(vehicle => vehicle.LicensePlateNumber)
            .ToListAsync(cancellationToken);
    }

    public Task<Vehicle?> GetVehicleByLicensePlateAsync(string licensePlate, CancellationToken cancellationToken = default) =>
        _context.Vehicles.FirstOrDefaultAsync(vehicle => vehicle.LicensePlateNumber == licensePlate, cancellationToken);

    public Task AddVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken = default) =>
        _context.Vehicles.AddAsync(vehicle, cancellationToken).AsTask();

    public Task<List<DeviceCatalog>> ListDeviceCatalogsAsync(CancellationToken cancellationToken = default) =>
        _context.DeviceCatalogs
            .AsNoTracking()
            .OrderBy(catalog => catalog.Name)
            .ToListAsync(cancellationToken);

    public Task<DeviceCatalog?> GetDeviceCatalogByNameAsync(string name, CancellationToken cancellationToken = default) =>
        _context.DeviceCatalogs.FirstOrDefaultAsync(catalog => catalog.Name == name, cancellationToken);

    public Task AddDeviceCatalogAsync(DeviceCatalog catalog, CancellationToken cancellationToken = default) =>
        _context.DeviceCatalogs.AddAsync(catalog, cancellationToken).AsTask();

    public Task<List<Device>> ListDevicesWithRoomAndCatalogAsync(int? buildingId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Devices
            .AsNoTracking()
            .Include(device => device.Room)
                .ThenInclude(room => room.Building)
            .Include(device => device.DeviceCatalog)
            .AsQueryable();

        if (buildingId.HasValue)
        {
            query = query.Where(device => device.Room.BuildingId == buildingId.Value);
        }

        return query
            .OrderBy(device => device.Room.RoomName)
            .ThenBy(device => device.DeviceName)
            .ToListAsync(cancellationToken);
    }

    public Task<Device?> GetDeviceAsync(int roomId, string deviceName, CancellationToken cancellationToken = default) =>
        _context.Devices.FirstOrDefaultAsync(device =>
            device.RoomId == roomId && device.DeviceName == deviceName, cancellationToken);

    public Task AddDeviceAsync(Device device, CancellationToken cancellationToken = default) =>
        _context.Devices.AddAsync(device, cancellationToken).AsTask();

    public Task<List<Service>> ListServicesWithRoomsAsync(CancellationToken cancellationToken = default) =>
        _context.Services
            .AsNoTracking()
            .Include(service => service.RoomServices)
                .ThenInclude(roomService => roomService.Room)
            .OrderBy(service => service.ServiceName)
            .ToListAsync(cancellationToken);

    public Task<Service?> GetServiceByNameAsync(string serviceName, CancellationToken cancellationToken = default) =>
        _context.Services.FirstOrDefaultAsync(service => service.ServiceName == serviceName, cancellationToken);

    public Task AddServiceAsync(Service service, CancellationToken cancellationToken = default) =>
        _context.Services.AddAsync(service, cancellationToken).AsTask();

    public Task<RoomService?> GetRoomServiceAsync(int roomId, int serviceId, CancellationToken cancellationToken = default) =>
        _context.RoomServices.FirstOrDefaultAsync(roomService =>
            roomService.RoomId == roomId && roomService.ServiceId == serviceId, cancellationToken);

    public Task AddRoomServiceAsync(RoomService roomService, CancellationToken cancellationToken = default) =>
        _context.RoomServices.AddAsync(roomService, cancellationToken).AsTask();

    public Task<List<Invoice>> ListInvoicesWithRoomAndPaymentsAsync(int? buildingId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Invoices
            .AsNoTracking()
            .Include(invoice => invoice.Room)
                .ThenInclude(room => room.Building)
            .Include(invoice => invoice.Payments)
            .AsQueryable();

        if (buildingId.HasValue)
        {
            query = query.Where(invoice => invoice.Room.BuildingId == buildingId.Value);
        }

        return query
            .OrderBy(invoice => invoice.Room.RoomName)
            .ThenBy(invoice => invoice.MonthYear)
            .ToListAsync(cancellationToken);
    }
}
