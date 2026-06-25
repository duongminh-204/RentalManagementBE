using Backend.Authorization;
using Backend.DTOs.Rooms;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = AuthorizationPolicies.ActiveOwnerSubscription)]
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
            if (!TryGetUserId(out var userId)) return Unauthorized();

            var rooms = await _roomService.GetAllRoomsAsync(buildingId, userId);
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
            try
            {
                var room = await _roomService.CreateRoomAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = room.RoomId }, new { data = room });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<RoomDto>> Update(int id, [FromBody] UpdateRoomDto dto)
        {
            try
            {
                var room = await _roomService.UpdateRoomAsync(id, dto);
                return Ok(new { data = room });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPatch("{id}/status")]
        public async Task<ActionResult<RoomDto>> UpdateStatus(int id, [FromBody] RoomStatusUpdateDto dto)
        {
            try
            {
                var room = await _roomService.UpdateRoomStatusAsync(id, dto.Status);
                return Ok(new { data = room });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _roomService.DeleteRoomAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("status/{status}")]
        public async Task<ActionResult<IEnumerable<RoomDto>>> GetByStatus(string status)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            var rooms = await _roomService.GetRoomsByStatusAsync(status, userId);
            return Ok(new { data = rooms });
        }

        [HttpGet("stats")]
        public async Task<ActionResult<RoomStatsDto>> GetStats([FromQuery] int? buildingId)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();

            var stats = await _roomService.GetRoomStatsAsync(buildingId, userId);
            return Ok(stats);
        }

        private bool TryGetUserId(out int userId)
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out userId);
        }
    }
}
