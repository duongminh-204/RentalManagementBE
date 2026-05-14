using Backend.DTOs;
using Backend.Entities;
using Backend.Interfaces;
using Backend.Repositories.Interfaces;
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
                Token = token
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

            var defaultRole = await _userRepository.GetRoleByNameAsync("Customer");
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

        private bool VerifyPassword(string inputPassword, string hashedPassword)
        {
            var result = _passwordHasher.VerifyHashedPassword(new User(), hashedPassword, inputPassword);
            return result == PasswordVerificationResult.Success;
        }
    }
}