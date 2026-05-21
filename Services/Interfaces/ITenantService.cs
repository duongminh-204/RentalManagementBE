using Backend.DTOs.Tenants;

namespace Backend.Services.Interfaces;

public interface ITenantService
{
    Task<IEnumerable<TenantListDto>> GetAllAsync(string? status = null, string? search = null);
    Task<TenantDetailDto?> GetByIdAsync(int id);
    Task<TenantDetailDto> CreateAsync(CreateTenantDto dto);
    Task<TenantDetailDto> UpdateAsync(int id, UpdateTenantDto dto);
    Task DeleteAsync(int id);
    Task<string> UploadIdCardAsync(int id, IFormFile file);
    Task<IEnumerable<TenantHistoryDto>> GetHistoryAsync(int id);
}
