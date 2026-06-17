using Backend.DTOs.Buildings;
using Backend.Entities;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;

namespace Backend.Services;

public class BuildingService : IBuildingService
{
    private readonly IBuildingRepository _repository;

    public BuildingService(IBuildingRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<BuildingDto>> GetAllBuildingsAsync(int? ownerUserId = null)
    {
        var buildings = await _repository.GetAllAsync(ownerUserId);
        return buildings.Select(MapToDto);
    }

    public async Task<BuildingDto?> GetBuildingByIdAsync(int id)
    {
        var building = await _repository.GetByIdAsync(id);
        return building != null ? MapToDto(building) : null;
    }

    public async Task<BuildingDto> CreateBuildingAsync(CreateBuildingDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.BuildingName))
            throw new InvalidOperationException("Tên tòa nhà là bắt buộc.");

        if (await _repository.GetByNameAsync(dto.BuildingName) != null)
            throw new InvalidOperationException("Tòa nhà này đã tồn tại.");

        var userId = dto.UserId ?? await _repository.GetAnyUserIdAsync();
        if (!userId.HasValue)
            throw new InvalidOperationException("Không có người dùng nào để gán tòa nhà. Vui lòng tạo một tài khoản người dùng trước.");

        var building = new Building
        {
            BuildingName = dto.BuildingName.Trim(),
            Address = dto.Address.Trim(),
            Description = dto.Description?.Trim(),
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            UserId = userId.Value
        };

        var created = await _repository.AddAsync(building);
        return MapToDto(created);
    }

    public async Task<BuildingDto> UpdateBuildingAsync(int id, UpdateBuildingDto dto)
    {
        var building = await _repository.GetByIdAsync(id);
        if (building == null)
            throw new KeyNotFoundException("Không tìm thấy tòa nhà.");

        if (string.IsNullOrWhiteSpace(dto.BuildingName))
            throw new InvalidOperationException("Tên tòa nhà là bắt buộc.");

        if (building.BuildingName != dto.BuildingName.Trim())
        {
            var existing = await _repository.GetByNameAsync(dto.BuildingName.Trim());
            if (existing != null && existing.BuildingId != id)
                throw new InvalidOperationException("Tên tòa nhà đã được sử dụng.");
        }

        building.BuildingName = dto.BuildingName.Trim();
        building.Address = dto.Address.Trim();
        building.Description = dto.Description?.Trim();
        building.Latitude = dto.Latitude;
        building.Longitude = dto.Longitude;

        if (dto.UserId.HasValue)
            building.UserId = dto.UserId.Value;

        await _repository.UpdateAsync(building);
        return MapToDto(building);
    }

    public async Task DeleteBuildingAsync(int id)
    {
        if (!await _repository.ExistsAsync(id))
            throw new KeyNotFoundException("Không tìm thấy tòa nhà.");

        await _repository.DeleteAsync(id);
    }

    private static BuildingDto MapToDto(Building building)
    {
        return new BuildingDto
        {
            BuildingId = building.BuildingId,
            BuildingName = building.BuildingName,
            Address = building.Address,
            Description = building.Description,
            Latitude = building.Latitude,
            Longitude = building.Longitude,
            UserId = building.UserId,
            CreatedAt = building.CreatedAt
        };
    }
}
