using Backend.Authorization;
using Backend.DTOs.Admin;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Controllers;

[Route("api/admin/subscriptions")]
[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public class AdminSubscriptionsController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminSubscriptionsController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<AdminSubscriptionDto>>> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
        => Ok(await _adminService.GetSubscriptionsAsync(status, search, page, pageSize));

    [HttpGet("grouped")]
    public async Task<ActionResult<PagedResultDto<AdminOwnerSubscriptionsGroupDto>>> GetGrouped(
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
        => Ok(await _adminService.GetSubscriptionsGroupedByOwnerAsync(status, search, page, pageSize));

    [HttpPost("{id}/upgrade")]
    public async Task<ActionResult<AdminSubscriptionDto>> Upgrade(int id, [FromBody] ChangePackageDto dto)
    {
        try { return Ok(await _adminService.UpgradeSubscriptionAsync(id, dto, GetUserId(), GetIp())); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("{id}/downgrade")]
    public async Task<ActionResult<AdminSubscriptionDto>> Downgrade(int id, [FromBody] ChangePackageDto dto)
    {
        try { return Ok(await _adminService.DowngradeSubscriptionAsync(id, dto, GetUserId(), GetIp())); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("{id}/renew")]
    public async Task<ActionResult<AdminSubscriptionDto>> Renew(int id)
    {
        try { return Ok(await _adminService.RenewSubscriptionAsync(id, GetUserId(), GetIp())); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("{id}/activate")]
    public async Task<ActionResult<AdminSubscriptionDto>> Activate(int id)
    {
        try { return Ok(await _adminService.ActivateSubscriptionAsync(id, GetUserId(), GetIp())); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("{id}/cancel")]
    public async Task<ActionResult<AdminSubscriptionDto>> Cancel(int id)
    {
        try { return Ok(await _adminService.CancelSubscriptionAsync(id, GetUserId(), GetIp())); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _adminService.DeleteSubscriptionAsync(id, GetUserId(), GetIp());
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    private int? GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : null;
    }

    private string? GetIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
