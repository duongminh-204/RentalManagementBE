using Backend.DTOs.Rooms;
using Backend.Entities;
using Backend.Interfaces;
using Backend.Repositories.Interfaces;

namespace Backend.Services;

public class RoomManagementService : IRoomManagementService
{
    private readonly IRoomManagementRepository _repo;

    public RoomManagementService(IRoomManagementRepository repo)
    {
        _repo = repo;
    }

    public async Task<IEnumerable<ServiceCatalogDto>> GetServiceCatalogAsync()
    {
        var services = await _repo.GetActiveServicesOrderedAsync();
        return services.Select(s => new ServiceCatalogDto
        {
            ServiceId = s.ServiceId,
            ServiceName = s.ServiceName,
            UnitPrice = s.UnitPrice,
            Unit = s.Unit,
            Description = s.Description,
            IsActive = s.IsActive
        });
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
            DeviceName = dto.DeviceName.Trim(),
            Quantity = dto.Quantity > 0 ? dto.Quantity : 1,
            Status = dto.Status ?? "Working",
            Note = dto.Note
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
        device.Quantity = dto.Quantity > 0 ? dto.Quantity : 1;
        device.Status = dto.Status ?? "Working";
        device.Note = dto.Note;
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
        {
            existing.Quantity = dto.Quantity > 0 ? dto.Quantity : 1;
            await _repo.SaveChangesAsync();
            return MapRoomService(existing, service);
        }

        var roomService = new RoomService
        {
            RoomId = roomId,
            ServiceId = dto.ServiceId,
            Quantity = dto.Quantity > 0 ? dto.Quantity : 1
        };
        _repo.AddRoomService(roomService);
        await _repo.SaveChangesAsync();
        return MapRoomService(roomService, service);
    }

    public async Task<RoomServiceItemDto> UpdateRoomServiceAsync(int roomId, int roomServiceId, UpdateRoomServiceDto dto)
    {
        var roomService = await _repo.GetRoomServiceWithServiceAsync(roomId, roomServiceId)
            ?? throw new KeyNotFoundException("Không tìm thấy dịch vụ phòng.");

        roomService.Quantity = dto.Quantity > 0 ? dto.Quantity : 1;
        await _repo.SaveChangesAsync();
        return MapRoomService(roomService, roomService.Service);
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
        DeviceName = d.DeviceName,
        Quantity = d.Quantity,
        Status = d.Status,
        Note = d.Note
    };

    private static RoomServiceItemDto MapRoomService(RoomService rs, Service s) => new()
    {
        RoomServiceId = rs.RoomServiceId,
        RoomId = rs.RoomId,
        ServiceId = rs.ServiceId,
        ServiceName = s.ServiceName,
        UnitPrice = s.UnitPrice,
        Unit = s.Unit,
        Quantity = rs.Quantity
    };
}
