using Backend.DTOs.Rooms;
using Backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]   // Bỏ comment sau khi có Auth
    public class RoomController : ControllerBase
    {
        private readonly IRoomService _roomService;

        public RoomController(IRoomService roomService)
        {
            _roomService = roomService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<RoomDto>>> GetAll([FromQuery] int? buildingId)
        {
            var rooms = await _roomService.GetAllRoomsAsync(buildingId);
            return Ok(new { data = rooms });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<RoomDetailDto>> GetById(int id)
        {
            var room = await _roomService.GetRoomByIdAsync(id);
            return room != null ? Ok(room) : NotFound();
        }

        [HttpPost]
        public async Task<ActionResult<RoomDto>> Create([FromBody] CreateRoomDto dto)
        {
            var room = await _roomService.CreateRoomAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = room.RoomId }, new { data = room });
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<RoomDto>> Update(int id, [FromBody] UpdateRoomDto dto)
        {
            var room = await _roomService.UpdateRoomAsync(id, dto);
            return Ok(new { data = room });
        }

        [HttpPatch("{id}/status")]
        public async Task<ActionResult<RoomDto>> UpdateStatus(int id, [FromBody] RoomStatusUpdateDto dto)
        {
            var room = await _roomService.UpdateRoomStatusAsync(id, dto.Status);
            return Ok(new { data = room });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _roomService.DeleteRoomAsync(id);
            return NoContent();
        }

        [HttpGet("status/{status}")]
        public async Task<ActionResult<IEnumerable<RoomDto>>> GetByStatus(string status)
        {
            var rooms = await _roomService.GetRoomsByStatusAsync(status);
            return Ok(new { data = rooms });
        }

        [HttpGet("stats")]
        public async Task<ActionResult<RoomStatsDto>> GetStats([FromQuery] int? buildingId)
        {
            var stats = await _roomService.GetRoomStatsAsync(buildingId);
            return Ok(stats);
        }
    }
}