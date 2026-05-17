using Backend.DTOs.Rooms;

namespace Backend.Interfaces;

public interface IRoomManagementService
{
    Task<IEnumerable<ServiceCatalogDto>> GetServiceCatalogAsync();
    Task<IEnumerable<TenantPickerDto>> GetTenantCandidatesAsync();
    Task<RoomImageDto> AddRoomImageAsync(int roomId, CreateRoomImageDto dto);
    Task DeleteRoomImageAsync(int roomId, int imageId);
    Task<RoomDeviceDto> AddDeviceAsync(int roomId, CreateDeviceDto dto);
    Task<RoomDeviceDto> UpdateDeviceAsync(int roomId, int deviceId, UpdateDeviceDto dto);
    Task DeleteDeviceAsync(int roomId, int deviceId);
    Task<RoomServiceItemDto> AssignServiceAsync(int roomId, AssignRoomServiceDto dto);
    Task<RoomServiceItemDto> UpdateRoomServiceAsync(int roomId, int roomServiceId, UpdateRoomServiceDto dto);
    Task DeleteRoomServiceAsync(int roomId, int roomServiceId);
    Task<TenantAssignmentDto> AssignTenantAsync(int roomId, AssignTenantDto dto);
    Task RemoveTenantAsync(int roomId, int contractId);
}
