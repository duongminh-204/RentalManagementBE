using Backend.Authorization;
using Backend.DTOs.Invoices;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Controllers;

[Route("api/invoices")]
[ApiController]
[Authorize(Policy = AuthorizationPolicies.ActiveOwner)]
[Authorize(Policy = PackageFeaturePolicies.PaymentInvoices)]
public class InvoiceController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;

    public InvoiceController(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    [HttpPost("utility-usage")]
    public async Task<ActionResult<InvoiceDto>> CreateInvoiceFromUtilityUsage([FromBody] CreateInvoiceFromUtilityUsageDto dto)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        dto.UserId = userId;
        var invoice = await _invoiceService.GenerateInvoiceFromUtilityUsageAsync(dto, userId);
        return Ok(invoice);
    }

    [HttpGet("{invoiceId}")]
    public async Task<ActionResult<InvoiceDto>> GetInvoice(int invoiceId)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var invoice = await _invoiceService.GetInvoiceByIdAsync(invoiceId, userId);
        return invoice == null ? NotFound() : Ok(invoice);
    }

    [HttpGet("room/{roomId}/month/{monthYear}")]
    public async Task<ActionResult<InvoiceDto>> GetInvoiceByRoomAndMonth(int roomId, string monthYear)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var invoice = await _invoiceService.GetInvoiceByRoomAndMonthAsync(roomId, monthYear, userId);
        return invoice == null ? NotFound() : Ok(invoice);
    }

    [HttpGet("history")]
    public async Task<ActionResult<IEnumerable<InvoiceDto>>> SearchInvoices(
        [FromQuery] int? roomId,
        [FromQuery] string? tenantName,
        [FromQuery] string? monthFrom,
        [FromQuery] string? monthTo,
        [FromQuery] string? status,
        [FromQuery] string? search)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var invoices = await _invoiceService.SearchInvoicesAsync(roomId, tenantName, monthFrom, monthTo, status, search, userId);
        return Ok(invoices);
    }

    [HttpPost("monthly")]
    public async Task<ActionResult<IEnumerable<InvoiceDto>>> GenerateInvoicesForMonth([FromQuery] string monthYear, [FromQuery] int? buildingId)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var invoices = await _invoiceService.GenerateInvoicesForMonthAsync(monthYear, buildingId, userId);
        return Ok(invoices);
    }

    [HttpDelete("{invoiceId}")]
    public async Task<IActionResult> DeleteInvoice(int invoiceId)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        try
        {
            await _invoiceService.DeleteInvoiceAsync(invoiceId, userId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    private bool TryGetUserId(out int userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out userId);
    }
}
