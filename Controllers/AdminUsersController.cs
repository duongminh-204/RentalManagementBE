using Backend.Authorization;
using Backend.DTOs.Admin;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Controllers;

[Route("api/admin/users")]
[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public class AdminUsersController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminUsersController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<AdminUserDto>>> GetAll(
        [FromQuery] string? role,
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] string? subscriptionStatus,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
        => Ok(await _adminService.GetUsersAsync(role, search, isActive, subscriptionStatus, page, pageSize));

    [HttpPost("{id}/enable")]
    public async Task<ActionResult<AdminUserDto>> Enable(int id)
    {
        try { return Ok(await _adminService.EnableUserAsync(id, GetUserId(), GetIp())); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpPost("{id}/disable")]
    public async Task<ActionResult<AdminUserDto>> Disable(int id)
    {
        try { return Ok(await _adminService.DisableUserAsync(id, GetUserId(), GetIp())); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("{id}/reset-password")]
    public async Task<ActionResult<AdminResetPasswordResultDto>> ResetPassword(int id)
    {
        try { return Ok(await _adminService.ResetUserPasswordAsync(id, GetUserId(), GetIp())); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpGet("{id}/password")]
    public async Task<ActionResult<AdminUserPasswordDto>> GetPassword(int id)
    {
        try { return Ok(await _adminService.GetUserPasswordAsync(id)); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpPut("{id}/password")]
    public async Task<IActionResult> ChangePassword(int id, [FromBody] AdminChangePasswordDto dto)
    {
        try
        {
            await _adminService.ChangeUserPasswordAsync(id, dto, GetUserId(), GetIp());
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

    [HttpPost("{id}/lock")]
    public async Task<ActionResult<AdminUserDto>> Lock(int id)
    {
        try { return Ok(await _adminService.DisableUserAsync(id, GetUserId(), GetIp())); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("{id}/unlock")]
    public async Task<ActionResult<AdminUserDto>> Unlock(int id)
    {
        try { return Ok(await _adminService.EnableUserAsync(id, GetUserId(), GetIp())); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _adminService.DeleteUserAsync(id, GetUserId(), GetIp());
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

    private int? GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var userId) ? userId : null;
    }

    private string? GetIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
