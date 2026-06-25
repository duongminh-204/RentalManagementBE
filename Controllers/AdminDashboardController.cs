using Backend.Authorization;
using Backend.DTOs.Admin;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[Route("api/admin/dashboard")]
[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public class AdminDashboardController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminDashboardController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<AdminDashboardSummaryDto>> GetSummary()
        => Ok(await _adminService.GetDashboardSummaryAsync());

    [HttpGet("charts")]
    public async Task<ActionResult<AdminDashboardChartsDto>> GetCharts()
        => Ok(await _adminService.GetDashboardChartsAsync());
}
