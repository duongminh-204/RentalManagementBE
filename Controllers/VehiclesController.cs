using Backend.DTOs.Vehicles;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Controllers;

[Route("api/vehicles")]
[ApiController]
[Authorize]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleService _vehicleService;

    public VehiclesController(IVehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<VehicleDto>>> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? type,
        [FromQuery] string? search,
        [FromQuery] int? buildingId)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var data = await _vehicleService.GetAllAsync(status, type, search, buildingId, userId);
        return Ok(data);
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<VehicleDto>>> Search([FromQuery] string licensePlate)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var data = await _vehicleService.SearchByLicensePlateAsync(licensePlate, userId);
        return Ok(data);
    }

    [HttpGet("unknown")]
    public async Task<ActionResult<IEnumerable<VehicleDto>>> GetUnknown()
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var data = await _vehicleService.GetUnknownAsync(userId);
        return Ok(data);
    }

    [HttpGet("parking-fee/summary")]
    public async Task<ActionResult<ParkingFeeSummaryDto>> GetParkingFeeSummary()
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var data = await _vehicleService.GetParkingFeeSummaryAsync(userId);
        return Ok(data);
    }

    [HttpGet("type")]
    public async Task<ActionResult<IEnumerable<VehicleDto>>> GetByType([FromQuery] string type)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var data = await _vehicleService.GetByTypeAsync(type, userId);
        return Ok(data);
    }

    [HttpGet("room/{roomId:int}")]
    public async Task<ActionResult<IEnumerable<VehicleDto>>> GetByRoom(int roomId)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var data = await _vehicleService.GetByRoomIdAsync(roomId, userId);
        return Ok(data);
    }

    [HttpGet("tenant/{tenantId:int}")]
    public async Task<ActionResult<IEnumerable<VehicleDto>>> GetByTenant(int tenantId)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var data = await _vehicleService.GetByTenantIdAsync(tenantId, userId);
        return Ok(data);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VehicleDto>> GetById(int id)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var vehicle = await _vehicleService.GetByIdAsync(id, userId);
        return vehicle != null ? Ok(vehicle) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<VehicleDto>> Create([FromBody] CreateVehicleDto dto)
    {
        try
        {
            var created = await _vehicleService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<VehicleDto>> Update(int id, [FromBody] UpdateVehicleDto dto)
    {
        try
        {
            var updated = await _vehicleService.UpdateAsync(id, dto);
            return Ok(updated);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _vehicleService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{id:int}/upload-image")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<UploadVehicleImageResponseDto>> UploadImage(int id, IFormFile file)
    {
        try
        {
            var path = await _vehicleService.UploadImageAsync(id, file);
            return Ok(new UploadVehicleImageResponseDto { ImageUrl = path });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private bool TryGetUserId(out int userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out userId);
    }
}
