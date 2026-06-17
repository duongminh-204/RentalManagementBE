using Backend.Entities;

namespace Backend.Repositories.Interfaces;

public interface IBuildingRepository
{
    Task<IEnumerable<Building>> GetAllAsync(int? ownerUserId = null);
    Task<Building?> GetByIdAsync(int id);
    Task<Building?> GetByNameAsync(string buildingName);
    Task<Building> AddAsync(Building building);
    Task UpdateAsync(Building building);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<int?> GetAnyUserIdAsync();
}
