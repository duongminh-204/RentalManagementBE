using Backend.Authorization;
using Backend.DTOs.Admin;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Controllers;

[Route("api/admin/audit-logs")]
[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public class AdminAuditLogsController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminAuditLogsController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<AdminAuditLogDto>>> GetAll(
        [FromQuery] int? userId,
        [FromQuery] string? action,
        [FromQuery] string? entity,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
        => Ok(await _adminService.GetAuditLogsAsync(userId, action, entity, from, to, page, pageSize));

    [HttpDelete]
    public async Task<ActionResult<object>> Clear(
        [FromQuery] int? userId,
        [FromQuery] string? action,
        [FromQuery] string? entity,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var deletedCount = await _adminService.ClearAuditLogsAsync(userId, action, entity, from, to, GetUserId(), GetIp());
        return Ok(new { deletedCount });
    }

    private int? GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : null;
    }

    private string? GetIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
