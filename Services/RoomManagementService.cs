using Backend.DTOs.Rooms;
using Backend.Entities;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Backend.Services;

public class RoomManagementService : IRoomManagementService
{
    private readonly IRoomManagementRepository _repo;
    private readonly IFileStorageService _fileStorage;

    public RoomManagementService(IRoomManagementRepository repo, IFileStorageService fileStorage)
    {
        _repo = repo;
        _fileStorage = fileStorage;
    }

    public async Task<IEnumerable<ServiceCatalogDto>> GetServiceCatalogAsync()
    {
        var services = await _repo.GetActiveServicesOrderedAsync();
        return services.Select(MapService);
    }

    public async Task<IEnumerable<TenantPickerDto>> GetTenantCandidatesAsync()
    {
        var tenants = await _repo.GetActiveTenantsOrderedAsync();
        return tenants.Select(t => new TenantPickerDto
        {
            TenantId = t.TenantId,
            FullName = t.FullName,
            PhoneNumber = t.PhoneNumber,
            Email = t.Email
        });
    }

    public async Task<RoomImageDto> AddRoomImageAsync(int roomId, CreateRoomImageDto dto)
    {
        await EnsureRoomExists(roomId);
        if (string.IsNullOrWhiteSpace(dto.ImageUrl))
            throw new InvalidOperationException("URL ảnh không được để trống.");

        var image = new RoomImage
        {
            RoomId = roomId,
            ImageUrl = dto.ImageUrl.Trim()
        };
        _repo.AddRoomImage(image);
        await _repo.SaveChangesAsync();

        return new RoomImageDto
        {
            RoomImageId = image.RoomImageId,
            RoomId = image.RoomId,
            ImageUrl = image.ImageUrl
        };
    }

    public async Task DeleteRoomImageAsync(int roomId, int imageId)
    {
        var image = await _repo.GetRoomImageAsync(roomId, imageId)
            ?? throw new KeyNotFoundException("Không tìm thấy ảnh phòng.");
        _repo.RemoveRoomImage(image);
        await _repo.SaveChangesAsync();
    }

    public async Task<RoomDeviceDto> AddDeviceAsync(int roomId, CreateDeviceDto dto)
    {
        await EnsureRoomExists(roomId);
        if (string.IsNullOrWhiteSpace(dto.DeviceName))
            throw new InvalidOperationException("Tên thiết bị không được để trống.");

        var device = new Device
        {
            RoomId = roomId,
            DeviceCatalogId = dto.DeviceCatalogId,
            DeviceName = dto.DeviceName.Trim(),
            Quantity = dto.Quantity > 0 ? dto.Quantity : 1,
            Status = dto.Status ?? "Working",
            ImageUrl = dto.ImageUrl
        };
        _repo.AddDevice(device);
        await _repo.SaveChangesAsync();

        return MapDevice(device);
    }

    public async Task<RoomDeviceDto> UpdateDeviceAsync(int roomId, int deviceId, UpdateDeviceDto dto)
    {
        var device = await _repo.GetDeviceAsync(roomId, deviceId)
            ?? throw new KeyNotFoundException("Không tìm thấy thiết bị.");

        device.DeviceName = dto.DeviceName.Trim();
        device.DeviceCatalogId = dto.DeviceCatalogId;
        device.Quantity = dto.Quantity > 0 ? dto.Quantity : 1;
        device.Status = dto.Status ?? "Working";
        device.ImageUrl = dto.ImageUrl;
        await _repo.SaveChangesAsync();
        return MapDevice(device);
    }

    public async Task DeleteDeviceAsync(int roomId, int deviceId)
    {
        var device = await _repo.GetDeviceAsync(roomId, deviceId)
            ?? throw new KeyNotFoundException("Không tìm thấy thiết bị.");
        _repo.RemoveDevice(device);
        await _repo.SaveChangesAsync();
    }

    public async Task<RoomServiceItemDto> AssignServiceAsync(int roomId, AssignRoomServiceDto dto)
    {
        await EnsureRoomExists(roomId);
        var service = await _repo.GetServiceByIdAsync(dto.ServiceId)
            ?? throw new KeyNotFoundException("Không tìm thấy dịch vụ.");

        var existing = await _repo.FindRoomServiceAsync(roomId, dto.ServiceId);
        if (existing != null)
            return MapRoomService(existing, service);

        var roomService = new RoomService
        {
            RoomId = roomId,
            ServiceId = dto.ServiceId
        };
        _repo.AddRoomService(roomService);
        await _repo.SaveChangesAsync();
        return MapRoomService(roomService, service);
    }

    public async Task DeleteRoomServiceAsync(int roomId, int roomServiceId)
    {
        var roomService = await _repo.GetRoomServiceWithServiceAsync(roomId, roomServiceId)
            ?? throw new KeyNotFoundException("Không tìm thấy dịch vụ phòng.");
        _repo.RemoveRoomService(roomService);
        await _repo.SaveChangesAsync();
    }

    public async Task<TenantAssignmentDto> AssignTenantAsync(int roomId, AssignTenantDto dto)
    {
        await EnsureRoomExists(roomId);
        var tenant = await _repo.GetTenantByIdAsync(dto.TenantId)
            ?? throw new KeyNotFoundException("Không tìm thấy khách thuê.");

        var contract = await _repo.FindActiveContractByTenantAndRoomAsync(dto.TenantId, roomId);
        if (contract == null)
            throw new InvalidOperationException(
                "Vui lòng tạo hợp đồng cho khách thuê và phòng này trước khi gán vào phòng.");

        var room = await _repo.GetRoomTrackedAsync(roomId);
        if (room != null && !string.Equals(room.Status, "Occupied", StringComparison.OrdinalIgnoreCase))
            room.Status = "Occupied";

        await _repo.SaveChangesAsync();

        return new TenantAssignmentDto
        {
            ContractId = contract.ContractId,
            TenantId = tenant.TenantId,
            FullName = tenant.FullName,
            PhoneNumber = tenant.PhoneNumber,
            Email = tenant.Email,
            StartDate = contract.StartDate,
            EndDate = contract.EndDate,
            Status = contract.Status
        };
    }

    public async Task RemoveTenantAsync(int roomId, int contractId)
    {
        var contract = await _repo.GetContractByRoomAndIdAsync(roomId, contractId)
            ?? throw new KeyNotFoundException("Không tìm thấy hợp đồng.");

        contract.Status = "Terminated";
        contract.EndDate = DateTime.UtcNow.Date;
        await _repo.SaveChangesAsync();

        if (!await _repo.RoomHasActiveContractAsync(roomId))
        {
            var room = await _repo.GetRoomTrackedAsync(roomId);
            if (room != null)
            {
                room.Status = "Available";
                await _repo.SaveChangesAsync();
            }
        }
    }

    private async Task EnsureRoomExists(int roomId)
    {
        if (!await _repo.RoomExistsAsync(roomId))
            throw new KeyNotFoundException("Không tìm thấy phòng.");
    }

    private static RoomDeviceDto MapDevice(Device d) => new()
    {
        DeviceId = d.DeviceId,
        RoomId = d.RoomId,
        DeviceCatalogId = d.DeviceCatalogId,
        DeviceName = d.DeviceName,
        Quantity = d.Quantity,
        Status = d.Status,
        ImageUrl = d.ImageUrl
    };

    private static RoomServiceItemDto MapRoomService(RoomService rs, Service s) => new()
    {
        RoomServiceId = rs.RoomServiceId,
        RoomId = rs.RoomId,
        ServiceId = rs.ServiceId,
        ServiceName = s.ServiceName,
        UnitPrice = s.UnitPrice,
        Unit = s.Unit
    };

    public async Task<RoomImageDto> UploadRoomImageAsync(int roomId, IFormFile file)
    {
        await EnsureRoomExists(roomId);

        if (file == null || file.Length == 0)
            throw new InvalidOperationException("File không hợp lệ.");

        if (file.Length > 5 * 1024 * 1024)
            throw new InvalidOperationException("Ảnh tối đa 5MB.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp"))
            throw new InvalidOperationException("Chỉ chấp nhận JPG, PNG, WEBP.");

        var fileName = $"{roomId}_{Guid.NewGuid():N}{ext}";
        var imageUrl = await _fileStorage.UploadFormFileAsync(file, "rooms", fileName);

        var image = new RoomImage
        {
            RoomId = roomId,
            ImageUrl = imageUrl
        };

        _repo.AddRoomImage(image);
        await _repo.SaveChangesAsync();

        return new RoomImageDto
        {
            RoomImageId = image.RoomImageId,
            RoomId = image.RoomId,
            ImageUrl = image.ImageUrl
        };
    }

    public async Task<RoomDeviceDto> UploadDeviceImageAsync(int roomId, int deviceId, IFormFile file)
    {
        await EnsureRoomExists(roomId);

        var device = await _repo.GetDeviceAsync(roomId, deviceId)
            ?? throw new KeyNotFoundException("Không tìm thấy thiết bị.");

        if (file == null || file.Length == 0)
            throw new InvalidOperationException("File không hợp lệ.");

        if (file.Length > 5 * 1024 * 1024)
            throw new InvalidOperationException("Ảnh tối đa 5MB.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp"))
            throw new InvalidOperationException("Chỉ chấp nhận JPG, PNG, WEBP.");

        var fileName = $"{roomId}_{deviceId}_{Guid.NewGuid():N}{ext}";
        device.ImageUrl = await _fileStorage.UploadFormFileAsync(file, "devices", fileName);
        await _repo.SaveChangesAsync();

        return MapDevice(device);
    }

    public async Task<ServiceCatalogDto> CreateServiceAsync(ServiceCatalogDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ServiceName))
            throw new InvalidOperationException("Tên dịch vụ không được để trống.");

        if (dto.UnitPrice < 0)
            throw new InvalidOperationException("Giá dịch vụ không hợp lệ.");

        var service = new Service
        {
            ServiceName = dto.ServiceName.Trim(),
            UnitPrice = dto.UnitPrice,
            BillingCycle = NormalizeBillingCycle(dto.BillingCycle),
            Unit = dto.Unit
        };

        _repo.AddService(service);
        await _repo.SaveChangesAsync();

        return MapService(service);
    }

    public async Task<ServiceCatalogDto> UpdateServiceAsync(int serviceId, ServiceCatalogDto dto)
    {
        var service = await _repo.GetServiceByIdAsync(serviceId)
            ?? throw new KeyNotFoundException("Không tìm thấy dịch vụ.");

        if (string.IsNullOrWhiteSpace(dto.ServiceName))
            throw new InvalidOperationException("Tên dịch vụ không được để trống.");

        if (dto.UnitPrice < 0)
            throw new InvalidOperationException("Giá dịch vụ không hợp lệ.");

        service.ServiceName = dto.ServiceName.Trim();
        service.UnitPrice = dto.UnitPrice;
        service.BillingCycle = NormalizeBillingCycle(dto.BillingCycle);
        service.Unit = dto.Unit;

        await _repo.SaveChangesAsync();

        return MapService(service);
    }

    private static string NormalizeBillingCycle(string? value)
    {
        var v = (value ?? string.Empty).Trim();
        return v.ToLowerInvariant() switch
        {
            "yearly" or "year" or "năm" or "nam" => "Yearly",
            "monthly" or "month" or "tháng" or "thang" or "" => "Monthly",
            _ => throw new InvalidOperationException("Chu kỳ tính giá chỉ nhận 'Monthly' (theo tháng) hoặc 'Yearly' (theo năm).")
        };
    }

    private static ServiceCatalogDto MapService(Service s) => new()
    {
        ServiceId = s.ServiceId,
        ServiceName = s.ServiceName,
        UnitPrice = s.UnitPrice,
        BillingCycle = s.BillingCycle,
        Unit = s.Unit,
        Icon = s.Icon
    };

    public async Task DeleteServiceAsync(int serviceId)
    {
        var service = await _repo.GetServiceByIdAsync(serviceId)
            ?? throw new KeyNotFoundException("Không tìm thấy dịch vụ.");

        _repo.RemoveService(service);
        await _repo.SaveChangesAsync();
    }

    public async Task<IEnumerable<DeviceCatalogDto>> GetDeviceCatalogAsync()
    {
        var items = await _repo.GetActiveDeviceCatalogsOrderedAsync();
        return items.Select(MapDeviceCatalog);
    }

    public async Task<DeviceCatalogDto> CreateDeviceCatalogAsync(DeviceCatalogDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidOperationException("Tên thiết bị không được để trống.");

        var name = dto.Name.Trim();
        if (await _repo.DeviceCatalogNameExistsAsync(name))
            throw new InvalidOperationException("Thiết bị này đã có trong danh mục.");

        var entity = new DeviceCatalog
        {
            Name = name,
            Icon = string.IsNullOrWhiteSpace(dto.Icon) ? null : dto.Icon.Trim()
        };

        _repo.AddDeviceCatalog(entity);
        await _repo.SaveChangesAsync();

        return MapDeviceCatalog(entity);
    }

    public async Task<DeviceCatalogDto> UpdateDeviceCatalogAsync(int deviceCatalogId, DeviceCatalogDto dto)
    {
        var entity = await _repo.GetDeviceCatalogByIdAsync(deviceCatalogId)
            ?? throw new KeyNotFoundException("Không tìm thấy thiết bị trong danh mục.");

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidOperationException("Tên thiết bị không được để trống.");

        var name = dto.Name.Trim();
        if (await _repo.DeviceCatalogNameExistsAsync(name, deviceCatalogId))
            throw new InvalidOperationException("Thiết bị này đã có trong danh mục.");

        entity.Name = name;
        entity.Icon = string.IsNullOrWhiteSpace(dto.Icon) ? null : dto.Icon.Trim();

        await _repo.SaveChangesAsync();

        return MapDeviceCatalog(entity);
    }

    public async Task DeleteDeviceCatalogAsync(int deviceCatalogId)
    {
        var entity = await _repo.GetDeviceCatalogByIdAsync(deviceCatalogId)
            ?? throw new KeyNotFoundException("Không tìm thấy thiết bị trong danh mục.");

        _repo.RemoveDeviceCatalog(entity);
        await _repo.SaveChangesAsync();
    }

    private static DeviceCatalogDto MapDeviceCatalog(DeviceCatalog d) => new()
    {
        DeviceCatalogId = d.DeviceCatalogId,
        Name = d.Name,
        Icon = d.Icon
    };
}
