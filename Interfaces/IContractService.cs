using Backend.DTOs.Contracts;

namespace Backend.Interfaces;

public interface IContractService
{
    Task<IEnumerable<ContractDto>> GetAllAsync(int? roomId = null, int? tenantId = null);
    Task<ContractDto?> GetByIdAsync(int id);
    Task<ContractDto> CreateAsync(CreateContractDto dto);
    Task<ContractDto> UpdateAsync(int id, UpdateContractDto dto);
    Task DeleteAsync(int id);
    Task<ContractDto> RenewAsync(int id, RenewContractDto dto);
    Task<string> UploadFileAsync(int id, IFormFile file);
}
