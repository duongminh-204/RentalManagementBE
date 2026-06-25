using Backend.Authorization;
using Backend.DTOs.Admin;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Controllers;

[Route("api/admin/packages")]
[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public class AdminPackagesController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminPackagesController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<AdminPackageDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] bool? isEnabled,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
        => Ok(await _adminService.GetPackagesAsync(search, isEnabled, page, pageSize));

    [HttpPost]
    public async Task<ActionResult<AdminPackageDto>> Create([FromBody] CreatePackageDto dto)
    {
        try
        {
            var created = await _adminService.CreatePackageAsync(dto, GetUserId(), GetIp());
            return CreatedAtAction(nameof(GetAll), created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<AdminPackageDto>> Update(int id, [FromBody] UpdatePackageDto dto)
    {
        try { return Ok(await _adminService.UpdatePackageAsync(id, dto, GetUserId(), GetIp())); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("{id}/enable")]
    public async Task<ActionResult<AdminPackageDto>> Enable(int id)
    {
        try { return Ok(await _adminService.EnablePackageAsync(id, GetUserId(), GetIp())); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpPost("{id}/disable")]
    public async Task<ActionResult<AdminPackageDto>> Disable(int id)
    {
        try { return Ok(await _adminService.DisablePackageAsync(id, GetUserId(), GetIp())); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    private int? GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : null;
    }

    private string? GetIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
