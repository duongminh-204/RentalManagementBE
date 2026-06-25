using Backend.Authorization;
using Backend.DTOs.Admin;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[Route("api/admin/payments")]
[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public class AdminPaymentsController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminPaymentsController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<AdminPaymentDto>>> GetHistory(
        [FromQuery] string? status,
        [FromQuery] int? ownerId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
        => Ok(await _adminService.GetPaymentsAsync(status, ownerId, from, to, page, pageSize));

    [HttpGet("revenue-report")]
    public async Task<ActionResult<RevenueReportDto>> GetRevenueReport(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
        => Ok(await _adminService.GetRevenueReportAsync(from, to));

    [HttpGet("export-excel")]
    public async Task<IActionResult> ExportExcel([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var bytes = await _adminService.ExportPaymentsExcelAsync(from, to);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"payments_{DateTime.Now:yyyyMMdd}.xlsx");
    }
}
