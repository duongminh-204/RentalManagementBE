using Backend.DTOs.Rooms;

namespace Backend.Services.Interfaces;

public interface IRoomService
{
    Task<IEnumerable<RoomDto>> GetAllRoomsAsync(int? buildingId = null, int? ownerUserId = null);
    Task<RoomDetailDto?> GetRoomByIdAsync(int id);
    Task<RoomDto> CreateRoomAsync(CreateRoomDto dto);
    Task<RoomDto> UpdateRoomAsync(int id, UpdateRoomDto dto);
    Task<RoomDto> UpdateRoomStatusAsync(int id, string status);
    Task DeleteRoomAsync(int id);
    Task<IEnumerable<RoomDto>> GetRoomsByStatusAsync(string status, int? ownerUserId = null);
    Task<RoomStatsDto> GetRoomStatsAsync(int? buildingId = null, int? ownerUserId = null);
}
