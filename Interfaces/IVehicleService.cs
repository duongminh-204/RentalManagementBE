using Backend.DTOs.Vehicles;
using Microsoft.AspNetCore.Http;

namespace Backend.Interfaces;

public interface IVehicleService
{
    Task<IEnumerable<VehicleDto>> GetAllAsync(string? status = null, string? type = null, string? search = null);
    Task<VehicleDto?> GetByIdAsync(int id);
    Task<IEnumerable<VehicleDto>> SearchByLicensePlateAsync(string licensePlate);
    Task<IEnumerable<VehicleDto>> GetByRoomIdAsync(int roomId);
    Task<IEnumerable<VehicleDto>> GetByTenantIdAsync(int tenantId);
    Task<IEnumerable<VehicleDto>> GetUnknownAsync();
    Task<ParkingFeeSummaryDto> GetParkingFeeSummaryAsync();
    Task<IEnumerable<VehicleDto>> GetByTypeAsync(string type);
    Task<VehicleDto> CreateAsync(CreateVehicleDto dto);
    Task<VehicleDto> UpdateAsync(int id, UpdateVehicleDto dto);
    Task DeleteAsync(int id);
    Task<string> UploadImageAsync(int id, IFormFile file);
}
