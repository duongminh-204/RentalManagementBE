using Backend.Authorization;
using Backend.DTOs.Tenants;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Controllers;

[Route("api/tenants")]
[ApiController]
[Authorize(Policy = AuthorizationPolicies.ActiveOwner)]
[Authorize(Policy = PackageFeaturePolicies.TenantManagement)]
public class TenantsController : ControllerBase
{
    private readonly ITenantService _tenantService;

    public TenantsController(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TenantListDto>>> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] string? q,
        [FromQuery] int? buildingId)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var term = search ?? q;
        var data = await _tenantService.GetAllAsync(status, term, buildingId, userId);
        return Ok(data);
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<TenantListDto>>> Search([FromQuery] string q)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var data = await _tenantService.GetAllAsync(null, q, null, userId);
        return Ok(data);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TenantDetailDto>> GetById(int id)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var tenant = await _tenantService.GetByIdAsync(id, userId);
        return tenant != null ? Ok(tenant) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<TenantDetailDto>> Create([FromBody] CreateTenantDto dto)
    {
        try
        {
            var created = await _tenantService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TenantDetailDto>> Update(int id, [FromBody] UpdateTenantDto dto)
    {
        try
        {
            var updated = await _tenantService.UpdateAsync(id, dto);
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

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _tenantService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{id}/upload-id-card")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<UploadIdCardResponseDto>> UploadIdCard(int id, IFormFile file)
    {
        try
        {
            var path = await _tenantService.UploadIdCardAsync(id, file);
            return Ok(new UploadIdCardResponseDto { IdCardImage = path });
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

    [HttpPost("{id}/upload-avatar")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<UploadAvatarResponseDto>> UploadAvatar(int id, IFormFile file)
    {
        try
        {
            var path = await _tenantService.UploadAvatarAsync(id, file);
            return Ok(new UploadAvatarResponseDto { Avatar = path });
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

    [HttpDelete("{id}/id-card")]
    public async Task<IActionResult> DeleteIdCard(int id)
    {
        try
        {
            await _tenantService.DeleteIdCardAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("{id}/history")]
    public async Task<ActionResult<IEnumerable<TenantHistoryDto>>> GetHistory(int id)
    {
        try
        {
            var history = await _tenantService.GetHistoryAsync(id);
            return Ok(history);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    private bool TryGetUserId(out int userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out userId);
    }
}
