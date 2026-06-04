using Backend.DTOs.Profile;
using Backend.Entities;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace Backend.Services;

public class ProfileService : IProfileService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IWebHostEnvironment _env;

    public ProfileService(
        IUserRepository users,
        IPasswordHasher<User> passwordHasher,
        IWebHostEnvironment env)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _env = env;
    }

    public async Task<ProfileDto> GetProfileAsync(int userId)
    {
        var user = await _users.GetUserByIdAsync(userId)
            ?? throw new KeyNotFoundException("Không tìm thấy người dùng.");

        return MapToDto(user);
    }

    public async Task<ProfileDto> UpdateProfileAsync(int userId, UpdateProfileDto dto)
    {
        var user = await _users.GetUserByIdAsync(userId)
            ?? throw new KeyNotFoundException("Không tìm thấy người dùng.");

        if (string.IsNullOrWhiteSpace(dto.FullName))
            throw new InvalidOperationException("Họ tên không được để trống.");

        var email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
        var phone = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim();

        if (await _users.IsEmailOrPhoneTakenByOtherAsync(email, phone, userId))
            throw new InvalidOperationException("Email hoặc số điện thoại đã được sử dụng.");

        user.FullName = dto.FullName.Trim();
        user.Email = email;
        user.PhoneNumber = phone;
        user.CCCD = string.IsNullOrWhiteSpace(dto.CCCD) ? null : dto.CCCD.Trim();
        user.Address = string.IsNullOrWhiteSpace(dto.Address) ? null : dto.Address.Trim();
        user.UpdatedAt = DateTime.Now;

        await _users.SaveChangesAsync();
        return MapToDto(user);
    }

    public async Task<string> UploadAvatarAsync(int userId, IFormFile file)
    {
        var user = await _users.GetUserByIdAsync(userId)
            ?? throw new KeyNotFoundException("Không tìm thấy người dùng.");

        if (file == null || file.Length == 0)
            throw new InvalidOperationException("File không hợp lệ.");

        if (file.Length > 5 * 1024 * 1024)
            throw new InvalidOperationException("Ảnh tối đa 5MB.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".jpg" or ".jpeg" or ".png"))
            throw new InvalidOperationException("Chỉ chấp nhận JPG, PNG.");

        var uploadsDir = Path.Combine(
            _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"),
            "uploads", "avatars");
        Directory.CreateDirectory(uploadsDir);

        if (!string.IsNullOrEmpty(user.Avatar))
            TryDeletePhysicalFile(user.Avatar, "avatars");

        var fileName = $"user_{userId}_{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        user.Avatar = $"/uploads/avatars/{fileName}";
        user.UpdatedAt = DateTime.Now;
        await _users.SaveChangesAsync();

        return user.Avatar;
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordDto dto)
    {
        var user = await _users.GetUserByIdAsync(userId)
            ?? throw new KeyNotFoundException("Không tìm thấy người dùng.");

        if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
            throw new InvalidOperationException("Mật khẩu mới phải có ít nhất 6 ký tự.");

        var verify = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.CurrentPassword);
        if (verify == PasswordVerificationResult.Failed)
            throw new InvalidOperationException("Mật khẩu hiện tại không chính xác.");

        user.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword);
        user.UpdatedAt = DateTime.Now;
        await _users.SaveChangesAsync();
    }

    private static ProfileDto MapToDto(User user) => new()
    {
        UserId = user.UserId,
        FullName = user.FullName,
        Email = user.Email,
        PhoneNumber = user.PhoneNumber,
        CCCD = user.CCCD,
        Address = user.Address,
        Avatar = user.Avatar,
        Role = user.Role?.Name ?? string.Empty,
        CreatedAt = user.CreatedAt
    };

    private void TryDeletePhysicalFile(string relativePath, string folder)
    {
        try
        {
            var fileName = Path.GetFileName(relativePath);
            if (string.IsNullOrEmpty(fileName)) return;

            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var fullPath = Path.GetFullPath(Path.Combine(webRoot, "uploads", folder, fileName));

            // Chống path-traversal
            if (!fullPath.StartsWith(Path.GetFullPath(webRoot), StringComparison.OrdinalIgnoreCase))
                return;

            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
        catch
        {
            // Bỏ qua lỗi khi dọn file cũ
        }
    }
}
