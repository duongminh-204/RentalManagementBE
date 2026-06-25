using Backend.Authorization;
using Backend.DTOs.Admin;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
}
