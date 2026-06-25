using Backend.Authorization;
using Backend.DTOs.Admin;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Controllers;

[Route("api/admin/owners")]
[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public class AdminOwnersController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly IOwnerFeatureService _ownerFeatureService;

    public AdminOwnersController(IAdminService adminService, IOwnerFeatureService ownerFeatureService)
    {
        _adminService = adminService;
        _ownerFeatureService = ownerFeatureService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<AdminOwnerListDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
        => Ok(await _adminService.GetOwnersAsync(search, status, page, pageSize));

    [HttpGet("{id}")]
    public async Task<ActionResult<AdminOwnerDetailDto>> GetById(int id)
    {
        var owner = await _adminService.GetOwnerByIdAsync(id);
        return owner != null ? Ok(owner) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<AdminOwnerDetailDto>> Create([FromBody] CreateAdminOwnerDto dto)
    {
        try
        {
            var created = await _adminService.CreateOwnerAsync(dto, GetUserId(), GetIp());
            return CreatedAtAction(nameof(GetById), new { id = created.OwnerId }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<AdminOwnerDetailDto>> Update(int id, [FromBody] UpdateAdminOwnerDto dto)
    {
        try
        {
            return Ok(await _adminService.UpdateOwnerAsync(id, dto, GetUserId(), GetIp()));
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
            await _adminService.DeleteOwnerAsync(id, GetUserId(), GetIp());
            return NoContent();
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

    [HttpPost("{id}/suspend")]
    public async Task<ActionResult<AdminOwnerDetailDto>> Suspend(int id)
    {
        try { return Ok(await _adminService.SuspendOwnerAsync(id, GetUserId(), GetIp())); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpPost("{id}/activate")]
    public async Task<ActionResult<AdminOwnerDetailDto>> Activate(int id)
    {
        try { return Ok(await _adminService.ActivateOwnerAsync(id, GetUserId(), GetIp())); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpPost("{id}/lock")]
    public async Task<ActionResult<AdminOwnerDetailDto>> Lock(int id)
    {
        try { return Ok(await _adminService.LockOwnerAsync(id, GetUserId(), GetIp())); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpPost("{id}/unlock")]
    public async Task<ActionResult<AdminOwnerDetailDto>> Unlock(int id)
    {
        try { return Ok(await _adminService.UnlockOwnerAsync(id, GetUserId(), GetIp())); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpGet("{id}/feature-grants")]
    public async Task<ActionResult<OwnerFeatureGrantsDto>> GetFeatureGrants(int id)
    {
        try
        {
            return Ok(await _ownerFeatureService.GetOwnerFeatureGrantsAsync(id));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPut("{id}/feature-grants")]
    public async Task<ActionResult<OwnerFeatureGrantsDto>> UpdateFeatureGrants(int id, [FromBody] UpdateOwnerFeatureGrantsDto dto)
    {
        try
        {
            return Ok(await _ownerFeatureService.UpdateOwnerFeatureGrantsAsync(id, dto, GetUserId(), GetIp()));
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

    private int? GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : null;
    }

    private string? GetIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
