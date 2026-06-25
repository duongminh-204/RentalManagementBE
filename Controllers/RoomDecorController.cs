using Backend.Authorization;
using Backend.DTOs.Rooms;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[Route("api/room-decor")]
[ApiController]
[Authorize(Policy = AuthorizationPolicies.ActiveOwnerSubscription)]
[Authorize(Policy = PackageFeaturePolicies.AiRoomDecor)]
public class RoomDecorController : ControllerBase
{
    private readonly IRoomDecorService _decor;

    public RoomDecorController(IRoomDecorService decor)
    {
        _decor = decor;
    }

    [HttpGet("styles")]
    public ActionResult<IEnumerable<RoomDecorStyleDto>> GetStyles()
    {
        return Ok(_decor.GetStyles());
    }

    [HttpGet("status")]
    public async Task<ActionResult<RoomDecorStatusDto>> GetStatus(CancellationToken cancellationToken)
    {
        return Ok(await _decor.GetStatusAsync(cancellationToken));
    }

    [HttpPost("generate")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<RoomDecorResultDto>> Generate(
        IFormFile file,
        [FromForm] string? styleId,
        [FromForm] string? customPrompt,
        [FromForm] int? roomId,
        [FromForm] bool saveToRoom = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _decor.GenerateAsync(
                file,
                styleId,
                customPrompt,
                roomId,
                saveToRoom,
                cancellationToken);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (TimeoutException ex)
        {
            return StatusCode(504, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
