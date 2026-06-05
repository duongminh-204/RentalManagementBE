using Backend.Data;
using Backend.Entities;
using Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories;

public class BuildingRepository : IBuildingRepository
{
    private readonly RentalManagementDb _context;

    public BuildingRepository(RentalManagementDb context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Building>> GetAllAsync()
    {
        return await _context.Buildings.AsNoTracking().OrderBy(b => b.BuildingName).ToListAsync();
    }

    public async Task<Building?> GetByIdAsync(int id)
    {
        return await _context.Buildings.FirstOrDefaultAsync(b => b.BuildingId == id);
    }

    public async Task<Building?> GetByNameAsync(string buildingName)
    {
        return await _context.Buildings.FirstOrDefaultAsync(b => b.BuildingName == buildingName);
    }

    public async Task<Building> AddAsync(Building building)
    {
        await _context.Buildings.AddAsync(building);
        await _context.SaveChangesAsync();
        return building;
    }

    public async Task UpdateAsync(Building building)
    {
        _context.Buildings.Update(building);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var building = await _context.Buildings.FindAsync(id);
        if (building != null)
        {
            _context.Buildings.Remove(building);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Buildings.AnyAsync(b => b.BuildingId == id);
    }

    public async Task<int?> GetAnyUserIdAsync()
    {
        return await _context.Users.OrderBy(u => u.UserId).Select(u => u.UserId).FirstOrDefaultAsync();
    }
}
