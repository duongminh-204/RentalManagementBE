using Backend.DTOs.Rooms;

namespace Backend.Services.Interfaces;

public interface IRoomManagementService
{
    Task<IEnumerable<ServiceCatalogDto>> GetServiceCatalogAsync();
    Task<IEnumerable<TenantPickerDto>> GetTenantCandidatesAsync();
    Task<RoomImageDto> AddRoomImageAsync(int roomId, CreateRoomImageDto dto);
    Task<RoomImageDto> UploadRoomImageAsync(int roomId, IFormFile file);
    Task DeleteRoomImageAsync(int roomId, int imageId);
    Task<RoomDeviceDto> AddDeviceAsync(int roomId, CreateDeviceDto dto);
    Task<RoomDeviceDto> UpdateDeviceAsync(int roomId, int deviceId, UpdateDeviceDto dto);
    Task DeleteDeviceAsync(int roomId, int deviceId);
    Task<RoomDeviceDto> UploadDeviceImageAsync(int roomId, int deviceId, IFormFile file);
    Task<RoomServiceItemDto> AssignServiceAsync(int roomId, AssignRoomServiceDto dto);
    Task DeleteRoomServiceAsync(int roomId, int roomServiceId);
    Task<TenantAssignmentDto> AssignTenantAsync(int roomId, AssignTenantDto dto);
    Task RemoveTenantAsync(int roomId, int contractId);
    Task<ServiceCatalogDto> CreateServiceAsync(ServiceCatalogDto dto);
    Task<ServiceCatalogDto> UpdateServiceAsync(int serviceId, ServiceCatalogDto dto);
    Task DeleteServiceAsync(int serviceId);
    Task<IEnumerable<DeviceCatalogDto>> GetDeviceCatalogAsync();
    Task<DeviceCatalogDto> CreateDeviceCatalogAsync(DeviceCatalogDto dto);
    Task<DeviceCatalogDto> UpdateDeviceCatalogAsync(int deviceCatalogId, DeviceCatalogDto dto);
    Task DeleteDeviceCatalogAsync(int deviceCatalogId);
}
