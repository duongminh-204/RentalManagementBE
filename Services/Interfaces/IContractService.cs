using Backend.DTOs.Contracts;

namespace Backend.Services.Interfaces;

public interface IContractService
{
    Task<IEnumerable<ContractDto>> GetAllAsync(
        int? roomId = null,
        int? tenantId = null,
        string? search = null,
        string? statusFilter = null,
        string? sortBy = null,
        bool sortDesc = true);

    Task<ContractDto?> GetByIdAsync(int id);

    Task<ContractDetailDto?> GetDetailAsync(int id);

    Task<IEnumerable<ContractDto>> GetExpiringAsync(int days);

    Task<IEnumerable<ContractReminderDto>> GetRemindersAsync();

    Task<ContractDto> CreateAsync(CreateContractDto dto);

    Task<ContractDto> UpdateAsync(int id, UpdateContractDto dto);

    Task DeleteAsync(int id);

    Task<ContractDto> RenewAsync(int id, RenewContractDto dto);

    Task<ContractDto> TerminateAsync(int id, TerminateContractDto dto);

    Task<ContractDto> UpdateDepositAsync(int id, UpdateDepositDto dto);

    Task<string> UploadFileAsync(int id, IFormFile file);

    Task<string> GenerateFromTemplateAsync(int id, GenerateContractDto? dto = null);
}
