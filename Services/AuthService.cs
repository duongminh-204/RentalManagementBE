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
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;

        private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(5);

        public AuthService(
            IUserRepository userRepository,
            JwtService jwtService,
            IPasswordHasher<User> passwordHasher,
            IConfiguration configuration,
            IMemoryCache cache)
        {
            _userRepository = userRepository;
            _jwtService = jwtService;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
            _cache = cache;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
        {
            var user = await _userRepository.GetUserByEmailAsync(request.Email);

            if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
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

            var token = _jwtService.GenerateToken(user);

            return new AuthResponseDto
            {
                IsSuccess = true,
                Message = "Đăng nhập thành công",
                Token = token,
                User = new AuthUserDto
                {
                    UserId = user.UserId,
                    FullName = user.FullName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Role = user.Role?.Name ?? string.Empty,
                }
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

            // Tạo user mới
            var newUser = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                PasswordHash = _passwordHasher.HashPassword(new User(), request.Password),
                RoleId = selectedRole.RoleId,
                IsActive = true
            };

            await _userRepository.AddUserAsync(newUser);

            newUser.Role = selectedRole;
            var token = _jwtService.GenerateToken(newUser);

            return new AuthResponseDto
            {
                IsSuccess = true,
                Message = "Đăng ký thành công.",
                Token = token,
                User = new AuthUserDto
                {
                    UserId = newUser.UserId,
                    FullName = newUser.FullName,
                    Email = newUser.Email,
                    PhoneNumber = newUser.PhoneNumber,
                    Role = selectedRole.Name,
                }
            };
        }

        private static string NormalizeRegisterRole(string? role)
        {
            return role?.Trim().ToLowerInvariant() switch
            {
                "owner" => "Owner",
                "tenant" => "Tenant",
                _ => "Tenant"
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

                // Ràng buộc audience theo Google ClientId nếu được cấu hình
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
                var defaultRole = await _userRepository.GetRoleByNameAsync("Tenant");
                if (defaultRole == null)
                {
                    return new AuthResponseDto
                    {
                        IsSuccess = false,
                        Message = "Không tìm thấy vai trò mặc định."
                    };
                }

                // Tạo user mới từ thông tin Google (mật khẩu ngẫu nhiên, không dùng để đăng nhập thường)
                user = new User
                {
                    FullName = string.IsNullOrWhiteSpace(payload.Name) ? payload.Email : payload.Name,
                    Email = payload.Email,
                    PasswordHash = _passwordHasher.HashPassword(new User(), Guid.NewGuid().ToString("N")),
                    Avatar = payload.Picture,
                    RoleId = defaultRole.RoleId,
                    IsActive = true
                };

                await _userRepository.AddUserAsync(user);
                user.Role = defaultRole;
            }
            else if (!user.IsActive)
            {
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "Tài khoản của bạn đã bị khóa."
                };
            }

            var token = _jwtService.GenerateToken(user);

            return new AuthResponseDto
            {
                IsSuccess = true,
                Message = "Đăng nhập thành công",
                Token = token,
                User = new AuthUserDto
                {
                    UserId = user.UserId,
                    FullName = user.FullName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Role = user.Role?.Name ?? string.Empty,
                }
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

            // Chỉ tạo & gửi OTP khi tài khoản tồn tại và đang hoạt động,
            // nhưng luôn trả về thông điệp chung để tránh dò email.
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

            user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
            user.UpdatedAt = DateTime.Now;
            await _userRepository.SaveChangesAsync();

            // OTP chỉ dùng một lần
            _cache.Remove(cacheKey);

            return new AuthResponseDto
            {
                IsSuccess = true,
                Message = "Đặt lại mật khẩu thành công."
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

        private bool VerifyPassword(string inputPassword, string hashedPassword)
        {
            var result = _passwordHasher.VerifyHashedPassword(new User(), hashedPassword, inputPassword);
            return result == PasswordVerificationResult.Success;
        }
    }
}
