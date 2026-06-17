using Backend.DTOs.Vehicles;
using Microsoft.AspNetCore.Http;

namespace Backend.Services.Interfaces;

public interface IVehicleService
{
    Task<IEnumerable<VehicleDto>> GetAllAsync(string? status = null, string? type = null, string? search = null, int? buildingId = null, int? ownerUserId = null);
    Task<VehicleDto?> GetByIdAsync(int id, int? ownerUserId = null);
    Task<IEnumerable<VehicleDto>> SearchByLicensePlateAsync(string licensePlate, int? ownerUserId = null);
    Task<IEnumerable<VehicleDto>> GetByRoomIdAsync(int roomId, int? ownerUserId = null);
    Task<IEnumerable<VehicleDto>> GetByTenantIdAsync(int tenantId, int? ownerUserId = null);
    Task<IEnumerable<VehicleDto>> GetUnknownAsync(int? ownerUserId = null);
    Task<ParkingFeeSummaryDto> GetParkingFeeSummaryAsync(int? ownerUserId = null);
    Task<IEnumerable<VehicleDto>> GetByTypeAsync(string type, int? ownerUserId = null);
    Task<VehicleDto> CreateAsync(CreateVehicleDto dto);
    Task<VehicleDto> UpdateAsync(int id, UpdateVehicleDto dto);
    Task DeleteAsync(int id);
    Task<string> UploadImageAsync(int id, IFormFile file);
}
