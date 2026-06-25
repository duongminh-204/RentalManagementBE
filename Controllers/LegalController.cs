using Backend.Authorization;
using Backend.DTOs.Legal;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Controllers;

[Route("api/legal")]
[ApiController]
[Authorize(Policy = AuthorizationPolicies.ActiveOwner)]
[Authorize(Policy = PackageFeaturePolicies.LegalChecklist)]
public class LegalController : ControllerBase
{
    private readonly ILegalService _legal;

    public LegalController(ILegalService legal)
    {
        _legal = legal;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<LegalDashboardDto>> GetDashboard(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok(await _legal.GetDashboardAsync(userId, ct));
    }

    [HttpGet("alerts")]
    public async Task<ActionResult<IEnumerable<LegalAlertDto>>> GetAlerts(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok(await _legal.GetAlertsAsync(userId, ct));
    }

    [HttpGet("tenants")]
    public async Task<ActionResult<IEnumerable<TenantLegalSummaryDto>>> GetTenants(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok(await _legal.GetTenantSummariesAsync(userId, ct));
    }

    [HttpGet("tenants/{id:int}")]
    public async Task<ActionResult<TenantLegalDetailDto>> GetTenant(int id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var detail = await _legal.GetTenantDetailAsync(id, userId, ct);
        return detail != null ? Ok(detail) : NotFound();
    }

    [HttpPut("tenants/{id:int}/profile")]
    public async Task<ActionResult<TenantLegalDetailDto>> UpdateTenantProfile(
        int id, [FromBody] UpdateTenantLegalProfileDto dto, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try
        {
            return Ok(await _legal.UpdateTenantProfileAsync(id, userId, dto, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("tenants/{id:int}/upload/{docType}")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<LegalUploadResponseDto>> UploadTenantDocument(
        int id, string docType, IFormFile file, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try
        {
            return Ok(await _legal.UploadTenantDocumentAsync(id, userId, docType, file, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("rooms")]
    public async Task<ActionResult<IEnumerable<RoomLegalSummaryDto>>> GetRooms(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok(await _legal.GetRoomSummariesAsync(userId, ct));
    }

    [HttpGet("rooms/{id:int}")]
    public async Task<ActionResult<RoomLegalDetailDto>> GetRoom(int id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var detail = await _legal.GetRoomDetailAsync(id, userId, ct);
        return detail != null ? Ok(detail) : NotFound();
    }

    [HttpPut("rooms/{id:int}/profile")]
    public async Task<ActionResult<RoomLegalDetailDto>> UpdateRoomProfile(
        int id, [FromBody] UpdateRoomLegalProfileDto dto, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try
        {
            return Ok(await _legal.UpdateRoomProfileAsync(id, userId, dto, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("rooms/{id:int}/upload/handover")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<LegalUploadResponseDto>> UploadRoomHandover(
        int id, IFormFile file, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try
        {
            return Ok(await _legal.UploadRoomHandoverAsync(id, userId, file, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("buildings/{buildingId:int}/documents")]
    public async Task<ActionResult<IEnumerable<BuildingLegalDocumentDto>>> GetBuildingDocuments(
        int buildingId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok(await _legal.GetBuildingDocumentsAsync(buildingId, userId, ct));
    }

    [HttpGet("documents")]
    public async Task<ActionResult<IEnumerable<BuildingLegalDocumentDto>>> GetAllDocuments(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok(await _legal.GetBuildingDocumentsAsync(null, userId, ct));
    }

    [HttpPost("buildings/{buildingId:int}/documents")]
    public async Task<ActionResult<BuildingLegalDocumentDto>> CreateBuildingDocument(
        int buildingId, [FromBody] CreateBuildingLegalDocumentDto dto, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try
        {
            var created = await _legal.CreateBuildingDocumentAsync(buildingId, userId, dto, ct);
            return CreatedAtAction(nameof(GetAllDocuments), created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("documents/{id:int}")]
    public async Task<ActionResult<BuildingLegalDocumentDto>> UpdateBuildingDocument(
        int id, [FromBody] UpdateBuildingLegalDocumentDto dto, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var updated = await _legal.UpdateBuildingDocumentAsync(id, userId, dto, ct);
        return updated != null ? Ok(updated) : NotFound();
    }

    [HttpDelete("documents/{id:int}")]
    public async Task<IActionResult> DeleteBuildingDocument(int id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return await _legal.DeleteBuildingDocumentAsync(id, userId, ct) ? NoContent() : NotFound();
    }

    [HttpPost("documents/{id:int}/upload")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<LegalUploadResponseDto>> UploadBuildingDocumentFile(
        int id, IFormFile file, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        try
        {
            return Ok(await _legal.UploadBuildingDocumentFileAsync(id, userId, file, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("sync-notifications")]
    public async Task<ActionResult<SyncNotificationsResultDto>> SyncNotifications(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok(await _legal.SyncNotificationsAsync(userId, ct));
    }

    private bool TryGetUserId(out int userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out userId);
    }
}
