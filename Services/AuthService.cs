using Backend.DTOs.Auth;
using Backend.Entities;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Backend.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly JwtService _jwtService;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AuthService(
            IUserRepository userRepository,
            JwtService jwtService,
            IPasswordHasher<User> passwordHasher)
        {
            _userRepository = userRepository;
            _jwtService = jwtService;
            _passwordHasher = passwordHasher;
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

        private bool VerifyPassword(string inputPassword, string hashedPassword)
        {
            var result = _passwordHasher.VerifyHashedPassword(new User(), hashedPassword, inputPassword);
            return result == PasswordVerificationResult.Success;
        }
    }
}
