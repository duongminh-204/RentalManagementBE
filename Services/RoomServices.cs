using Backend.DTOs.Rooms;
using Backend.Entities;
using Backend.Services.Interfaces;
using RoomRepositoryInterface = Backend.Interfaces.IRoomRepository;

namespace Backend.Services
{
    public class RoomServices : IRoomService
    {
        private readonly RoomRepositoryInterface _repository;

        public RoomServices(RoomRepositoryInterface repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<RoomDto>> GetAllRoomsAsync(int? buildingId = null)
        {
            var rooms = await _repository.GetAllAsync(buildingId);
            return rooms.Select(MapToDto);
        }

        public async Task<RoomDetailDto?> GetRoomByIdAsync(int id)
        {
            var room = await _repository.GetByIdAsync(id);
            return room != null ? MapToDetailDto(room) : null;
        }

        public async Task<RoomDto> CreateRoomAsync(CreateRoomDto dto)
        {
            // Kiểm tra Building có tồn tại không
            if (!await _repository.BuildingExistsAsync(dto.BuildingId))
                throw new KeyNotFoundException($"Không tìm thấy tòa nhà với ID = {dto.BuildingId}.");

            var existing = await _repository.GetByRoomNumberAsync(dto.RoomNumber, dto.BuildingId);
            if (existing != null)
                throw new InvalidOperationException($"Phòng {dto.RoomNumber} đã tồn tại trong tòa nhà này.");

            var room = new Room
            {
                RoomName = dto.RoomNumber,
                Price = dto.RentalPrice,
                ElectricPrice = dto.ElectricPrice,
                WaterPrice = dto.WaterPrice,
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

            // Kiểm tra Building có tồn tại không
            if (!await _repository.BuildingExistsAsync(dto.BuildingId))
                throw new KeyNotFoundException($"Không tìm thấy tòa nhà với ID = {dto.BuildingId}.");

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
            room.Description = dto.Description;
            room.Status = dto.Status;
            room.BuildingId = dto.BuildingId;
            room.MaxPeople = dto.MaxPeople;
            room.Area = dto.Area;   
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

        private static IEnumerable<Contract> GetRoomContracts(Room room) =>
            room.Contracts.Where(c =>
                string.Equals(c.Status, "Active", StringComparison.OrdinalIgnoreCase));

        private static RoomDto MapToDto(Room room)
        {
            return new RoomDto
            {
                RoomId = room.RoomId,
                RoomNumber = room.RoomName,
                RoomName = room.RoomName,
                RentalPrice = room.Price,
                Price = room.Price,
                ElectricPrice = room.ElectricPrice,
                WaterPrice = room.WaterPrice,
                Description = room.Description,
                Status = room.Status,
                BuildingId = room.BuildingId,
                Area = room.Area,
                MaxPeople = room.MaxPeople,
                CreatedAt = room.CreatedAt,
                UpdatedAt = room.CreatedAt
            };
        }

        private static RoomDetailDto MapToDetailDto(Room room)
        {
            var dto = new RoomDetailDto
            {
                RoomId = room.RoomId,
                RoomNumber = room.RoomName,
                RoomName = room.RoomName,
                RentalPrice = room.Price,
                Price = room.Price,
                ElectricPrice = room.ElectricPrice,
                WaterPrice = room.WaterPrice,
                Description = room.Description,
                Status = room.Status,
                BuildingId = room.BuildingId,
                Area = room.Area,
                MaxPeople = room.MaxPeople,
                CreatedAt = room.CreatedAt,
                UpdatedAt = room.CreatedAt,
                RoomImages = room.RoomImages
                    .Select(img => new RoomImageDto
                    {
                        RoomImageId = img.RoomImageId,
                        RoomId = img.RoomId,
                        ImageUrl = img.ImageUrl
                    })
                    .ToList(),
                Devices = room.Devices
                    .Select(d => new RoomDeviceDto
                    {
                        DeviceId = d.DeviceId,
                        RoomId = d.RoomId,
                        DeviceCatalogId = d.DeviceCatalogId,
                        DeviceName = d.DeviceName,
                        Quantity = d.Quantity,
                        Status = d.Status,
                        ImageUrl = d.ImageUrl,
                        Icon = d.DeviceCatalog?.Icon
                    })
                    .ToList(),
                Tenants = GetRoomContracts(room)
                    .Where(c => c.Tenant != null)
                    .GroupBy(c => c.TenantId)
                    .Select(g => g.First())
                    .Select(c => new RoomTenantDto
                    {
                        ContractId = c.ContractId,
                        TenantId = c.Tenant!.TenantId,
                        FullName = c.Tenant.FullName,
                        PhoneNumber = c.Tenant.PhoneNumber,
                        Email = c.Tenant.Email
                    })
                    .ToList(),
                RoomServices = room.RoomServices
                    .Select(rs => new RoomServiceItemDto
                    {
                        RoomServiceId = rs.RoomServiceId,
                        RoomId = rs.RoomId,
                        ServiceId = rs.ServiceId,
                        ServiceName = rs.Service.ServiceName,
                        UnitPrice = rs.Service.UnitPrice,
                        BillingCycle = rs.Service.BillingCycle,
                        Unit = rs.Service.Unit,
                        Icon = rs.Service.Icon
                    })
                    .ToList()
            };

            return dto;
        }
    }
}
