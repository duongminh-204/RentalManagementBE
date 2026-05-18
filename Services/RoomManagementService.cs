using Backend.Data;
using Backend.DTOs.Rooms;
using Backend.Entities;
using Backend.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class RoomManagementService : IRoomManagementService
{
    private readonly RentalManagementDb _context;

    public RoomManagementService(RentalManagementDb context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ServiceCatalogDto>> GetServiceCatalogAsync()
    {
        return await _context.Services
            .Where(s => s.IsActive)
            .OrderBy(s => s.ServiceName)
            .Select(s => new ServiceCatalogDto
            {
                ServiceId = s.ServiceId,
                ServiceName = s.ServiceName,
                UnitPrice = s.UnitPrice,
                Unit = s.Unit,
                Description = s.Description,
                IsActive = s.IsActive
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<TenantPickerDto>> GetTenantCandidatesAsync()
    {
        return await _context.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.FullName)
            .Select(u => new TenantPickerDto
            {
                UserId = u.UserId,
                FullName = u.FullName,
                Avatar = u.Avatar,
                PhoneNumber = u.PhoneNumber,
                Email = u.Email
            })
            .ToListAsync();
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
        _context.RoomImages.Add(image);
        await _context.SaveChangesAsync();

        return new RoomImageDto
        {
            RoomImageId = image.RoomImageId,
            RoomId = image.RoomId,
            ImageUrl = image.ImageUrl
        };
    }

    public async Task DeleteRoomImageAsync(int roomId, int imageId)
    {
        var image = await _context.RoomImages
            .FirstOrDefaultAsync(i => i.RoomImageId == imageId && i.RoomId == roomId)
            ?? throw new KeyNotFoundException("Không tìm thấy ảnh phòng.");
        _context.RoomImages.Remove(image);
        await _context.SaveChangesAsync();
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
        _context.Devices.Add(device);
        await _context.SaveChangesAsync();

        return MapDevice(device);
    }

    public async Task<RoomDeviceDto> UpdateDeviceAsync(int roomId, int deviceId, UpdateDeviceDto dto)
    {
        var device = await _context.Devices
            .FirstOrDefaultAsync(d => d.DeviceId == deviceId && d.RoomId == roomId)
            ?? throw new KeyNotFoundException("Không tìm thấy thiết bị.");

        device.DeviceName = dto.DeviceName.Trim();
        device.Quantity = dto.Quantity > 0 ? dto.Quantity : 1;
        device.Status = dto.Status ?? "Working";
        device.Note = dto.Note;
        await _context.SaveChangesAsync();
        return MapDevice(device);
    }

    public async Task DeleteDeviceAsync(int roomId, int deviceId)
    {
        var device = await _context.Devices
            .FirstOrDefaultAsync(d => d.DeviceId == deviceId && d.RoomId == roomId)
            ?? throw new KeyNotFoundException("Không tìm thấy thiết bị.");
        _context.Devices.Remove(device);
        await _context.SaveChangesAsync();
    }

    public async Task<RoomServiceItemDto> AssignServiceAsync(int roomId, AssignRoomServiceDto dto)
    {
        await EnsureRoomExists(roomId);
        var service = await _context.Services.FindAsync(dto.ServiceId)
            ?? throw new KeyNotFoundException("Không tìm thấy dịch vụ.");

        var existing = await _context.RoomServices
            .FirstOrDefaultAsync(rs => rs.RoomId == roomId && rs.ServiceId == dto.ServiceId);
        if (existing != null)
        {
            existing.Quantity = dto.Quantity > 0 ? dto.Quantity : 1;
            await _context.SaveChangesAsync();
            return MapRoomService(existing, service);
        }

        var roomService = new RoomService
        {
            RoomId = roomId,
            ServiceId = dto.ServiceId,
            Quantity = dto.Quantity > 0 ? dto.Quantity : 1
        };
        _context.RoomServices.Add(roomService);
        await _context.SaveChangesAsync();
        return MapRoomService(roomService, service);
    }

    public async Task<RoomServiceItemDto> UpdateRoomServiceAsync(int roomId, int roomServiceId, UpdateRoomServiceDto dto)
    {
        var roomService = await _context.RoomServices
            .Include(rs => rs.Service)
            .FirstOrDefaultAsync(rs => rs.RoomServiceId == roomServiceId && rs.RoomId == roomId)
            ?? throw new KeyNotFoundException("Không tìm thấy dịch vụ phòng.");

        roomService.Quantity = dto.Quantity > 0 ? dto.Quantity : 1;
        await _context.SaveChangesAsync();
        return MapRoomService(roomService, roomService.Service);
    }

    public async Task DeleteRoomServiceAsync(int roomId, int roomServiceId)
    {
        var roomService = await _context.RoomServices
            .FirstOrDefaultAsync(rs => rs.RoomServiceId == roomServiceId && rs.RoomId == roomId)
            ?? throw new KeyNotFoundException("Không tìm thấy dịch vụ phòng.");
        _context.RoomServices.Remove(roomService);
        await _context.SaveChangesAsync();
    }

    public async Task<TenantAssignmentDto> AssignTenantAsync(int roomId, AssignTenantDto dto)
    {
        await EnsureRoomExists(roomId);
        var user = await _context.Users.FindAsync(dto.UserId)
            ?? throw new KeyNotFoundException("Không tìm thấy người dùng.");

        var start = dto.StartDate ?? DateTime.UtcNow.Date;
        var end = dto.EndDate ?? start.AddYears(1);

        var contract = new Contract
        {
            RoomId = roomId,
            UserId = dto.UserId,
            StartDate = start,
            EndDate = end,
            Deposit = dto.Deposit,
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        };
        _context.Contracts.Add(contract);

        var room = await _context.Rooms.FindAsync(roomId);
        if (room != null && !string.Equals(room.Status, "Occupied", StringComparison.OrdinalIgnoreCase))
        {
            room.Status = "Occupied";
        }

        await _context.SaveChangesAsync();

        return new TenantAssignmentDto
        {
            ContractId = contract.ContractId,
            UserId = user.UserId,
            FullName = user.FullName,
            Avatar = user.Avatar,
            PhoneNumber = user.PhoneNumber,
            Email = user.Email,
            StartDate = contract.StartDate,
            EndDate = contract.EndDate,
            Status = contract.Status
        };
    }

    public async Task RemoveTenantAsync(int roomId, int contractId)
    {
        var contract = await _context.Contracts
            .FirstOrDefaultAsync(c => c.ContractId == contractId && c.RoomId == roomId);

        if (contract == null)
            throw new KeyNotFoundException("Không tìm thấy hợp đồng.");

        contract.Status = "Terminated";

        _context.Contracts.Update(contract);

        await _context.SaveChangesAsync();

        var hasActive = await _context.Contracts
            .AnyAsync(c => c.RoomId == roomId && c.Status == "Active");

        if (!hasActive)
        {
            var room = await _context.Rooms.FindAsync(roomId);

            if (room != null)
            {
                room.Status = "Available";
                await _context.SaveChangesAsync();
            }
        }
    }
    private async Task EnsureRoomExists(int roomId)
    {
        if (!await _context.Rooms.AnyAsync(r => r.RoomId == roomId))
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
