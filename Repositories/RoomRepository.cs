using Backend.Data;
using Backend.DTOs.Rooms;
using Backend.Entities;
using Backend.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories
{
    public class RoomRepository : IRoomRepository
    {
        private readonly RentalManagementDb _context;

        public RoomRepository(RentalManagementDb context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Room>> GetAllAsync(int? buildingId = null)
        {
            var query = _context.Rooms.Include(r => r.Building).AsQueryable();

            if (buildingId.HasValue)
                query = query.Where(r => r.BuildingId == buildingId.Value);

            return await query.ToListAsync();
        }

        public async Task<Room?> GetByIdAsync(int id)
        {
            return await _context.Rooms
                .Include(r => r.Building)
                .Include(r => r.RoomImages)
                .Include(r => r.Devices)
                .Include(r => r.RoomServices)
                    .ThenInclude(rs => rs.Service)
                .Include(r => r.Contracts)
                    .ThenInclude(c => c.Tenant)
                .FirstOrDefaultAsync(r => r.RoomId == id);
        }

        public async Task<Room?> GetByRoomNumberAsync(string roomNumber, int buildingId)
        {
            return await _context.Rooms
                .FirstOrDefaultAsync(r => r.RoomName == roomNumber && r.BuildingId == buildingId);
        }

        public async Task<IEnumerable<Room>> GetByStatusAsync(string status)
        {
            return await _context.Rooms
                .Where(r => r.Status == status)
                .ToListAsync();
        }

        public async Task<Room> AddAsync(Room room)
        {
            await _context.Rooms.AddAsync(room);
            await _context.SaveChangesAsync();
            return room;
        }

        public async Task UpdateAsync(Room room)
        {
            _context.Rooms.Update(room);
            room.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room != null)
            {
                _context.Rooms.Remove(room);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Rooms.AnyAsync(r => r.RoomId == id);
        }

        public async Task<RoomStatsDto> GetStatsAsync(int? buildingId = null)
        {
            var query = _context.Rooms.AsQueryable();
            if (buildingId.HasValue)
                query = query.Where(r => r.BuildingId == buildingId.Value);

            var list = await query.ToListAsync();

            return new RoomStatsDto
            {
                Total = list.Count,
                Occupied = list.Count(r => r.Status == "occupied"),
                Vacant = list.Count(r => r.Status == "vacant"),
                Maintenance = list.Count(r => r.Status == "maintenance")
            };
        }
    }
}