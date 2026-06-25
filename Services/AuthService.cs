using Backend.Authorization;
using Backend.DTOs.Auth;
using Backend.Entities;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;

namespace Backend.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly JwtService _jwtService;
        private readonly UserManager<User> _userManager;
        private readonly IUserRoleService _userRoleService;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;

        private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(5);

        public AuthService(
            IUserRepository userRepository,
            JwtService jwtService,
            UserManager<User> userManager,
            IUserRoleService userRoleService,
            IConfiguration configuration,
            IMemoryCache cache)
        {
            _userRepository = userRepository;
            _jwtService = jwtService;
            _userManager = userManager;
            _userRoleService = userRoleService;
            _configuration = configuration;
            _cache = cache;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
        {
            var user = await _userRepository.GetUserByEmailAsync(request.Email);

            if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
            {
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "Email hoặc mật khẩu không chính xác."
                };
            }

            if (!user.IsActive)
            {
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "Tài khoản của bạn đã bị khóa."
                };
            }

            if (user.IsSuspended)
            {
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "Tài khoản của bạn đã bị tạm ngưng."
                };
            }

            var roles = await _userRoleService.GetRolesAsync(user);
            var token = _jwtService.GenerateToken(user, roles);

            return new AuthResponseDto
            {
                IsSuccess = true,
                Message = "Đăng nhập thành công",
                Token = token,
                User = await MapAuthUserAsync(user)
            };
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
        {
            var isExist = await _userRepository.IsEmailOrPhoneExistAsync(request.Email, request.PhoneNumber);
            if (isExist)
            {
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "Email hoặc Số điện thoại đã được sử dụng."
                };
            }

            var selectedRoleName = NormalizeRegisterRole(request.Role);
            var selectedRole = await _userRepository.GetRoleByNameAsync(selectedRoleName);
            if (selectedRole == null)
            {
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "Không tìm thấy vai trò đã chọn."
                };
            }

            var newUser = new User
            {
                UserName = request.Email.Trim(),
                FullName = request.FullName,
                Email = request.Email.Trim(),
                PhoneNumber = request.PhoneNumber,
                IsActive = true
            };

            var createResult = await _userManager.CreateAsync(newUser, request.Password);
            if (!createResult.Succeeded)
            {
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = string.Join(" ", createResult.Errors.Select(e => e.Description))
                };
            }

            await _userManager.AddToRoleAsync(newUser, selectedRoleName);
            var roles = await _userRoleService.GetRolesAsync(newUser);
            var token = _jwtService.GenerateToken(newUser, roles);

            return new AuthResponseDto
            {
                IsSuccess = true,
                Message = "Đăng ký thành công.",
                Token = token,
                User = await MapAuthUserAsync(newUser)
            };
        }

        private static string NormalizeRegisterRole(string? role)
        {
            return role?.Trim().ToLowerInvariant() switch
            {
                "owner" => RoleNames.Owner,
                "tenant" => RoleNames.Tenant,
                _ => RoleNames.Tenant
            };
        }

        public async Task<AuthResponseDto> GoogleLoginAsync(GoogleLoginRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Credential))
            {
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "Thiếu Google credential."
                };
            }

            GoogleJsonWebSignature.Payload payload;
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings();

                var clientId = _configuration["Google:ClientId"];
                if (!string.IsNullOrWhiteSpace(clientId))
                    settings.Audience = new[] { clientId };

                payload = await GoogleJsonWebSignature.ValidateAsync(request.Credential, settings);
            }
            catch (InvalidJwtException)
            {
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "Google credential không hợp lệ."
                };
            }

            if (string.IsNullOrWhiteSpace(payload.Email))
            {
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "Không lấy được email từ tài khoản Google."
                };
            }

            var user = await _userRepository.GetUserByEmailAsync(payload.Email);

            if (user == null)
            {
                var defaultRole = await _userRepository.GetRoleByNameAsync(RoleNames.Tenant);
                if (defaultRole == null)
                {
                    return new AuthResponseDto
                    {
                        IsSuccess = false,
                        Message = "Không tìm thấy vai trò mặc định."
                    };
                }

                user = new User
                {
                    UserName = payload.Email,
                    FullName = string.IsNullOrWhiteSpace(payload.Name) ? payload.Email : payload.Name,
                    Email = payload.Email,
                    PasswordHash = _userManager.PasswordHasher.HashPassword(new User(), Guid.NewGuid().ToString("N")),
                    Avatar = payload.Picture,
                    IsActive = true
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    return new AuthResponseDto
                    {
                        IsSuccess = false,
                        Message = string.Join(" ", createResult.Errors.Select(e => e.Description))
                    };
                }

                await _userManager.AddToRoleAsync(user, RoleNames.Tenant);
            }
            else if (!user.IsActive)
            {
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "Tài khoản của bạn đã bị khóa."
                };
            }

            var roles = await _userRoleService.GetRolesAsync(user);
            var token = _jwtService.GenerateToken(user, roles);

            return new AuthResponseDto
            {
                IsSuccess = true,
                Message = "Đăng nhập thành công",
                Token = token,
                User = await MapAuthUserAsync(user)
            };
        }

        public async Task<AuthResponseDto> ForgotPasswordAsync(ForgotPasswordRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "Vui lòng nhập email."
                };
            }

            var email = request.Email.Trim();
            var user = await _userRepository.GetUserByEmailAsync(email);

            if (user != null && user.IsActive)
            {
                var otp = GenerateOtp();

                _cache.Set(GetOtpCacheKey(email), otp, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = OtpLifetime
                });

                await SendOtpEmailAsync(user.Email!, otp);
            }

            return new AuthResponseDto
            {
                IsSuccess = true,
                Message = "Nếu email tồn tại trong hệ thống, mã OTP đã được gửi tới hộp thư của bạn."
            };
        }

        public async Task<AuthResponseDto> ResetPasswordAsync(ResetPasswordRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Otp) ||
                string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "Vui lòng nhập đầy đủ email, mã OTP và mật khẩu mới."
                };
            }

            if (request.NewPassword.Length < 6)
            {
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "Mật khẩu mới phải có ít nhất 6 ký tự."
                };
            }

            var email = request.Email.Trim();
            var cacheKey = GetOtpCacheKey(email);

            if (!_cache.TryGetValue(cacheKey, out string? storedOtp) || storedOtp != request.Otp.Trim())
            {
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "Mã OTP không đúng hoặc đã hết hạn."
                };
            }

            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user == null)
            {
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "Không tìm thấy người dùng."
                };
            }

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, request.NewPassword);
            if (!resetResult.Succeeded)
            {
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = string.Join(" ", resetResult.Errors.Select(e => e.Description))
                };
            }

            user.UpdatedAt = DateTime.Now;
            await _userRepository.SaveChangesAsync();

            _cache.Remove(cacheKey);

            return new AuthResponseDto
            {
                IsSuccess = true,
                Message = "Đặt lại mật khẩu thành công."
            };
        }

        private async Task<AuthUserDto> MapAuthUserAsync(User user)
        {
            return new AuthUserDto
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = await _userRoleService.GetPrimaryRoleAsync(user) ?? string.Empty,
            };
        }

        private static string GetOtpCacheKey(string email) => $"pwd-reset-otp:{email.ToLowerInvariant()}";

        private static string GenerateOtp() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

        private async Task SendOtpEmailAsync(string toEmail, string otp)
        {
            var host = _configuration["Smtp:Host"];
            var fromAddress = _configuration["Smtp:From"] ?? _configuration["Smtp:Username"];

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromAddress))
                throw new InvalidOperationException("Cấu hình SMTP (Smtp:Host, Smtp:From) chưa được thiết lập.");

            var port = int.TryParse(_configuration["Smtp:Port"], out var p) ? p : 587;
            var enableSsl = !bool.TryParse(_configuration["Smtp:EnableSsl"], out var ssl) || ssl;
            var username = _configuration["Smtp:Username"];
            var password = _configuration["Smtp:Password"];

            using var message = new MailMessage
            {
                From = new MailAddress(fromAddress),
                Subject = "Mã OTP đặt lại mật khẩu",
                Body = $"Mã OTP đặt lại mật khẩu của bạn là: {otp}\n\nMã có hiệu lực trong 5 phút. Vui lòng không chia sẻ mã này cho bất kỳ ai.",
                IsBodyHtml = false
            };
            message.To.Add(toEmail);

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                Credentials = string.IsNullOrWhiteSpace(username)
                    ? null
                    : new NetworkCredential(username, password)
            };

            await client.SendMailAsync(message);
        }
    }
}
