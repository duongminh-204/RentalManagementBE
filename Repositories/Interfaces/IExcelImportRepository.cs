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
    Task<List<Building>> ListBuildingsWithOwnerAsync(CancellationToken cancellationToken = default);

    Task<Dictionary<string, Room>> GetRoomsByBuildingAsync(int buildingId, Func<string?, string> normalizeKey, CancellationToken cancellationToken = default);
    Task AddRoomAsync(Room room, CancellationToken cancellationToken = default);
    Task<List<Room>> ListRoomsWithDetailsAsync(int? buildingId = null, CancellationToken cancellationToken = default);

    Task<List<Tenant>> GetTenantsWithContractsAsync(CancellationToken cancellationToken = default);
    Task AddTenantAsync(Tenant tenant, CancellationToken cancellationToken = default);
    Task<List<Tenant>> ListTenantsWithVehiclesAndContractsAsync(CancellationToken cancellationToken = default);

    Task<Contract?> GetActiveContractAsync(int roomId, int tenantId, CancellationToken cancellationToken = default);
    Task AddContractAsync(Contract contract, CancellationToken cancellationToken = default);
    Task<List<Contract>> ListContractsWithRoomAndTenantAsync(int? buildingId = null, CancellationToken cancellationToken = default);

    Task<List<Invoice>> GetInvoicesWithPaymentsAsync(IEnumerable<int> roomIds, CancellationToken cancellationToken = default);
    Task AddInvoiceAsync(Invoice invoice, CancellationToken cancellationToken = default);
    void RemovePayments(IEnumerable<Payment> payments);

    Task<List<Vehicle>> ListVehiclesWithRoomAndTenantAsync(int? buildingId = null, CancellationToken cancellationToken = default);
    Task<Vehicle?> GetVehicleByLicensePlateAsync(string licensePlate, CancellationToken cancellationToken = default);
    Task AddVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken = default);

    Task<List<DeviceCatalog>> ListDeviceCatalogsAsync(CancellationToken cancellationToken = default);
    Task<DeviceCatalog?> GetDeviceCatalogByNameAsync(string name, CancellationToken cancellationToken = default);
    Task AddDeviceCatalogAsync(DeviceCatalog catalog, CancellationToken cancellationToken = default);

    Task<List<Device>> ListDevicesWithRoomAndCatalogAsync(int? buildingId = null, CancellationToken cancellationToken = default);
    Task<Device?> GetDeviceAsync(int roomId, string deviceName, CancellationToken cancellationToken = default);
    Task AddDeviceAsync(Device device, CancellationToken cancellationToken = default);

    Task<List<Service>> ListServicesWithRoomsAsync(CancellationToken cancellationToken = default);
    Task<Service?> GetServiceByNameAsync(string serviceName, CancellationToken cancellationToken = default);
    Task AddServiceAsync(Service service, CancellationToken cancellationToken = default);
    Task<RoomService?> GetRoomServiceAsync(int roomId, int serviceId, CancellationToken cancellationToken = default);
    Task AddRoomServiceAsync(RoomService roomService, CancellationToken cancellationToken = default);

    Task<List<Invoice>> ListInvoicesWithRoomAndPaymentsAsync(int? buildingId = null, CancellationToken cancellationToken = default);
}
