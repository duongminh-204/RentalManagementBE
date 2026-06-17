using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs.Profile;

public class ProfileDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? CCCD { get; set; }
    public string? Address { get; set; }
    public string? Avatar { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class UpdateProfileDto
{
    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [MaxLength(20)]
    public string? CCCD { get; set; }

    public string? Address { get; set; }
}

public class ChangePasswordDto
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;
}

public class UploadAvatarResponseDto
{
    public string Avatar { get; set; } = string.Empty;
}
