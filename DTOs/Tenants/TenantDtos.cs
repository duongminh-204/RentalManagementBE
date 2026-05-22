namespace Backend.DTOs.Tenants;

public class TenantListDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Cccd { get; set; }
    public string? IdCardImage { get; set; }
    public string? Avatar { get; set; }
    public string? Address { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Occupation { get; set; }
    public string? Workplace { get; set; }
    public bool IsActive { get; set; }
    public string Status { get; set; } = "inactive";
    public int? RoomId { get; set; }
    public string? RoomNumber { get; set; }
    public int? ContractId { get; set; }
    public DateTime? MoveInDate { get; set; }
    public DateTime? MoveOutDate { get; set; }
    public decimal Deposit { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TenantDetailDto : TenantListDto
{
    public List<TenantHistoryDto> History { get; set; } = new();
}

public class TenantHistoryDto
{
    public int ContractId { get; set; }
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Deposit { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateTenantDto
{
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Cccd { get; set; }
    public string? Address { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Occupation { get; set; }
    public string? Workplace { get; set; }
    public int? RoomId { get; set; }
    public DateTime? MoveInDate { get; set; }
    public DateTime? MoveOutDate { get; set; }
    public decimal Deposit { get; set; }
    public string? Notes { get; set; }
    public string? Password { get; set; }
}

public class UpdateTenantDto : CreateTenantDto
{
    public bool IsActive { get; set; } = true;
    public string? Status { get; set; }
}

public class UploadIdCardResponseDto
{
    public string IdCardImage { get; set; } = string.Empty;
}

public class UploadAvatarResponseDto
{
    public string Avatar { get; set; } = string.Empty;
}
