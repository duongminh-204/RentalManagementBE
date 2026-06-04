using Backend.DTOs.Auth;
using Backend.Entities;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace Backend.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly JwtService _jwtService;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IConfiguration _configuration;

        public AuthService(
            IUserRepository userRepository,
            JwtService jwtService,
            IPasswordHasher<User> passwordHasher,
            IConfiguration configuration)
        {
            _userRepository = userRepository;
            _jwtService = jwtService;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
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

            var defaultRole = await _userRepository.GetRoleByNameAsync("Tenant");
            if (defaultRole == null)
            {
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "Không tìm thấy vai trò mặc định."
                };
            }

            // Tạo user mới
            var newUser = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                PasswordHash = _passwordHasher.HashPassword(new User(), request.Password),
                RoleId = defaultRole.RoleId,
                IsActive = true
            };

            await _userRepository.AddUserAsync(newUser);

            return new AuthResponseDto
            {
                IsSuccess = true,
                Message = "Đăng ký thành công."
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

        private bool VerifyPassword(string inputPassword, string hashedPassword)
        {
            var result = _passwordHasher.VerifyHashedPassword(new User(), hashedPassword, inputPassword);
            return result == PasswordVerificationResult.Success;
        }
    }
}
