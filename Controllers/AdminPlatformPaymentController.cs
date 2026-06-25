using Backend.Authorization;
using Backend.DTOs.Package;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Controllers;

[Route("api/admin/payment-settings")]
[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public class AdminPlatformPaymentController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminPlatformPaymentController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet]
    public async Task<ActionResult<PlatformPaymentSettingDto>> Get()
        => Ok(await _adminService.GetPlatformPaymentSettingsAsync());

    [HttpPut]
    public async Task<ActionResult<PlatformPaymentSettingDto>> Update([FromBody] UpdatePlatformPaymentSettingDto dto)
    {
        try
        {
            return Ok(await _adminService.UpdatePlatformPaymentSettingsAsync(dto, GetUserId(), GetIp()));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Không thể lưu cấu hình.", detail = ex.Message });
        }
    }

    private int? GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : null;
    }

    private string? GetIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
