using Backend.DTOs.Profile;
using Microsoft.AspNetCore.Http;

namespace Backend.Services.Interfaces;

public interface IProfileService
{
    Task<ProfileDto> GetProfileAsync(int userId);
    Task<ProfileDto> UpdateProfileAsync(int userId, UpdateProfileDto dto);
    Task<string> UploadAvatarAsync(int userId, IFormFile file);
    Task ChangePasswordAsync(int userId, ChangePasswordDto dto);
}
