using Backend.Authorization;
using System.Security.Claims;
using Backend.DTOs.Profile;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[Route("api/profile")]
[ApiController]
[Authorize(Policy = AuthorizationPolicies.ActiveUser)]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet("me")]
    public async Task<ActionResult<ProfileDto>> GetMe()
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        try
        {
            var profile = await _profileService.GetProfileAsync(userId);
            return Ok(profile);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPut("me")]
    public async Task<ActionResult<ProfileDto>> UpdateMe([FromBody] UpdateProfileDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (!TryGetUserId(out var userId))
            return Unauthorized();

        try
        {
            var updated = await _profileService.UpdateProfileAsync(userId, dto);
            return Ok(updated);
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

    [HttpPost("avatar")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<UploadAvatarResponseDto>> UploadAvatar(IFormFile file)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        try
        {
            var path = await _profileService.UploadAvatarAsync(userId, file);
            return Ok(new UploadAvatarResponseDto { Avatar = path });
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

    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (!TryGetUserId(out var userId))
            return Unauthorized();

        try
        {
            await _profileService.ChangePasswordAsync(userId, dto);
            return Ok(new { message = "Đổi mật khẩu thành công." });
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

    private bool TryGetUserId(out int userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out userId);
    }
}
