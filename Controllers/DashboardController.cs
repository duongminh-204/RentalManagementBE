using Backend.Authorization;
using Backend.Services.Interfaces;
using Backend.DTOs.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Controllers;

[Route("api/dashboard")]
[ApiController]
[Authorize(Policy = AuthorizationPolicies.ActiveOwnerSubscription)]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly IExcelImportService _excelImportService;

    public DashboardController(IDashboardService dashboardService, IExcelImportService excelImportService)
    {
        _dashboardService = dashboardService;
        _excelImportService = excelImportService;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats([FromQuery] int? buildingId, [FromQuery] int? month, [FromQuery] int? year)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var today = DateTime.Today;
        var data = await _dashboardService.GetDashboardStatsAsync(month ?? today.Month, year ?? today.Year, buildingId, userId);
        return Ok(data);
    }

    [HttpGet("rooms/stats")]
    public async Task<IActionResult> GetRoomStats([FromQuery] int? buildingId)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var data = await _dashboardService.GetRoomStatsAsync(buildingId, userId);
        return Ok(data);
    }

    [HttpGet("debt/info")]
    [Authorize(Policy = PackageFeaturePolicies.RevenueDebtReports)]
    public async Task<IActionResult> GetDebtInfo([FromQuery] int? buildingId)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var data = await _dashboardService.GetDebtInfoAsync(buildingId, userId);
        return Ok(data);
    }

    [HttpPost("debt/invoices/{invoiceId:int}/payments")]
    [Authorize(Policy = PackageFeaturePolicies.RevenueDebtReports)]
    public async Task<IActionResult> RecordDebtPayment(int invoiceId, [FromBody] DashboardDebtPaymentRequestDto request)
    {
        try
        {
            var data = await _dashboardService.RecordDebtPaymentAsync(invoiceId, request);
            return Ok(data);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("debt/invoices/{invoiceId:int}/items/{itemKey}/restore")]
    [Authorize(Policy = PackageFeaturePolicies.RevenueDebtReports)]
    public async Task<IActionResult> RestoreDebtItem(int invoiceId, string itemKey)
    {
        try
        {
            var data = await _dashboardService.RestoreDebtItemAsync(invoiceId, itemKey);
            return Ok(data);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("revenue/{month:int}/{year:int}")]
    [Authorize(Policy = PackageFeaturePolicies.RevenueDebtReports)]
    public async Task<IActionResult> GetRevenue(int month, int year, [FromQuery] int? buildingId)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var data = await _dashboardService.GetRevenueAsync(month, year, buildingId, userId);
        return Ok(data);
    }

    [HttpPost("import-excel")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> ImportExcel(IFormFile file, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _excelImportService.ImportDashboardSeedAsync(file, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("import/template-file")]
    [Authorize(Policy = AuthorizationPolicies.OwnerOrAdmin)]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadTemplateFile(IFormFile file, CancellationToken cancellationToken)
    {
        try
        {
            await _excelImportService.SaveTemplateFileAsync(file, cancellationToken);
            return Ok(new { message = "Đã lưu mẫu Excel thành công." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("import/template")]
    public async Task<IActionResult> DownloadTemplate(CancellationToken cancellationToken)
    {
        var template = await _excelImportService.GetTemplateFileAsync(cancellationToken);
        return File(
            template.Content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            template.FileName);
    }

    [HttpGet("export-excel")]
    public async Task<IActionResult> ExportDashboardExcel([FromQuery] int? buildingId, [FromQuery] int? month, [FromQuery] int? year)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var today = DateTime.Today;
        var selectedMonth = month ?? today.Month;
        var selectedYear = year ?? today.Year;

        var exportFile = await _dashboardService.ExportDashboardExcelAsync(selectedMonth, selectedYear, buildingId, userId);
        return File(
            exportFile.Content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            exportFile.FileName);
    }

    private bool TryGetUserId(out int userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out userId);
    }
}
