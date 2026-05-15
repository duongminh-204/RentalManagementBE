using Backend.DTOs.Rooms;
using Backend.Entities;
using Backend.Interfaces;

namespace Backend.Services
{
    public class RoomServices : IRoomService
    {
        private readonly IRoomRepository _repository;

        public RoomServices(IRoomRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<RoomDto>> GetAllRoomsAsync(int? buildingId = null)
        {
            var rooms = await _repository.GetAllAsync(buildingId);
            return rooms.Select(MapToDto);
        }

        public async Task<RoomDto?> GetRoomByIdAsync(int id)
        {
            var room = await _repository.GetByIdAsync(id);
            return room != null ? MapToDto(room) : null;
        }

        public async Task<RoomDto> CreateRoomAsync(CreateRoomDto dto)
        {
            var existing = await _repository.GetByRoomNumberAsync(dto.RoomNumber, dto.BuildingId);
            if (existing != null)
                throw new InvalidOperationException($"Phòng {dto.RoomNumber} đã tồn tại trong tòa nhà này.");

            var room = new Room
            {
                RoomName = dto.RoomNumber,
                Price = dto.RentalPrice,
                ElectricPrice = dto.ElectricPrice,
                WaterPrice = dto.WaterPrice,
                InternetPrice = dto.InternetPrice,
                Description = dto.Description,
                Status = dto.Status,
                BuildingId = dto.BuildingId
            };

            var created = await _repository.AddAsync(room);
            return MapToDto(created);
        }

        public async Task<RoomDto> UpdateRoomAsync(int id, UpdateRoomDto dto)
        {
            var room = await _repository.GetByIdAsync(id);
            if (room == null) throw new KeyNotFoundException("Không tìm thấy phòng.");

            // Kiểm tra trùng số phòng (nếu thay đổi)
            if (room.RoomName != dto.RoomNumber)
            {
                var existing = await _repository.GetByRoomNumberAsync(dto.RoomNumber, dto.BuildingId);
                if (existing != null && existing.RoomId != id)
                    throw new InvalidOperationException($"Phòng {dto.RoomNumber} đã tồn tại.");
            }

            room.RoomName = dto.RoomNumber;
            room.Price = dto.RentalPrice;
            room.ElectricPrice = dto.ElectricPrice;
            room.WaterPrice = dto.WaterPrice;
            room.InternetPrice = dto.InternetPrice;
            room.Description = dto.Description;
            room.Status = dto.Status;
            room.BuildingId = dto.BuildingId;

            await _repository.UpdateAsync(room);
            return MapToDto(room);
        }

        public async Task<RoomDto> UpdateRoomStatusAsync(int id, string status)
        {
            var room = await _repository.GetByIdAsync(id);
            if (room == null) throw new KeyNotFoundException("Không tìm thấy phòng.");

            room.Status = status;
            await _repository.UpdateAsync(room);
            return MapToDto(room);
        }

        public async Task DeleteRoomAsync(int id)
        {
            if (!await _repository.ExistsAsync(id))
                throw new KeyNotFoundException("Không tìm thấy phòng.");

            await _repository.DeleteAsync(id);
        }

        public async Task<IEnumerable<RoomDto>> GetRoomsByStatusAsync(string status)
        {
            var rooms = await _repository.GetByStatusAsync(status);
            return rooms.Select(MapToDto);
        }

        public async Task<RoomStatsDto> GetRoomStatsAsync(int? buildingId = null)
        {
            return await _repository.GetStatsAsync(buildingId);
        }

        private static RoomDto MapToDto(Room room)
        {
            return new RoomDto
            {
                RoomId = room.RoomId,
                RoomNumber = room.RoomName,
                RentalPrice = room.Price,
                ElectricPrice = room.ElectricPrice,
                WaterPrice = room.WaterPrice,
                InternetPrice = room.InternetPrice,
     
                Description = room.Description,
                Status = room.Status,
                BuildingId = room.BuildingId,
                CreatedAt = room.CreatedAt,
                UpdatedAt = room.CreatedAt
            };
        }
    }
}