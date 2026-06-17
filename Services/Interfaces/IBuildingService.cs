using Backend.DTOs.Buildings;

namespace Backend.Services.Interfaces;

public interface IBuildingService
{
    Task<IEnumerable<BuildingDto>> GetAllBuildingsAsync(int? ownerUserId = null);
    Task<BuildingDto?> GetBuildingByIdAsync(int id);
    Task<BuildingDto> CreateBuildingAsync(CreateBuildingDto dto);
    Task<BuildingDto> UpdateBuildingAsync(int id, UpdateBuildingDto dto);
    Task DeleteBuildingAsync(int id);
}
