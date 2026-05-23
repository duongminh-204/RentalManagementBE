using Backend.DTOs.Buildings;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BuildingController : ControllerBase
{
    private readonly IBuildingService _buildingService;

    public BuildingController(IBuildingService buildingService)
    {
        _buildingService = buildingService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var buildings = await _buildingService.GetAllBuildingsAsync();
        return Ok(buildings);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var building = await _buildingService.GetBuildingByIdAsync(id);
        if (building == null)
            return NotFound();

        return Ok(building);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBuildingDto dto)
    {
        try
        {
            var created = await _buildingService.CreateBuildingAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.BuildingId }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateBuildingDto dto)
    {
        try
        {
            var updated = await _buildingService.UpdateBuildingAsync(id, dto);
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
            await _buildingService.DeleteBuildingAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
