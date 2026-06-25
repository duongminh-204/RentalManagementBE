using Backend.DTOs.Auth;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IAuditLogService _auditLogService;

        public AuthController(IAuthService authService, IAuditLogService auditLogService)
        {
            _authService = authService;
            _auditLogService = auditLogService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.LoginAsync(request);

            if (result.IsSuccess && result.User != null)
            {
                await _auditLogService.LogAsync(
                    result.User.UserId,
                    "Login",
                    "User",
                    result.User.UserId,
                    result.User.Email,
                    HttpContext.Connection.RemoteIpAddress?.ToString());
            }

            return result.IsSuccess
                ? Ok(result)
                : Unauthorized(new { message = result.Message });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.RegisterAsync(request);

            return result.IsSuccess
                ? Ok(result)
                : BadRequest(new { message = result.Message });
        }

        [HttpPost("google")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.GoogleLoginAsync(request);

            return result.IsSuccess
                ? Ok(result)
                : Unauthorized(new { message = result.Message });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.ForgotPasswordAsync(request);

            return result.IsSuccess
                ? Ok(result)
                : BadRequest(new { message = result.Message });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.ResetPasswordAsync(request);

            return result.IsSuccess
                ? Ok(result)
                : BadRequest(new { message = result.Message });
        }
    }
}