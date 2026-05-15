using Backend.DTOs.Rooms;
using Backend.Entities;

namespace Backend.Interfaces
{
    public interface IRoomRepository
    {
        Task<IEnumerable<Room>> GetAllAsync(int? buildingId = null);
        Task<Room?> GetByIdAsync(int id);
        Task<Room?> GetByRoomNumberAsync(string roomNumber, int buildingId);
        Task<IEnumerable<Room>> GetByStatusAsync(string status);
        Task<Room> AddAsync(Room room);
        Task UpdateAsync(Room room);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<RoomStatsDto> GetStatsAsync(int? buildingId = null);
    }
}