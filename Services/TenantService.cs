using Backend.DTOs.Tenants;
using Backend.Entities;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Backend.Services;

public class TenantService : ITenantService
{
    private readonly ITenantRepository _tenants;
    private readonly IFileStorageService _fileStorage;

    public TenantService(ITenantRepository tenants, IFileStorageService fileStorage)
    {
        _tenants = tenants;
        _fileStorage = fileStorage;
    }

    public async Task<IEnumerable<TenantListDto>> GetAllAsync(string? status = null, string? search = null, int? buildingId = null, int? ownerUserId = null)
    {
        var list = await _tenants.ListWithContractsAndRoomsAsync(ownerUserId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim().ToLowerInvariant();
            list = list.Where(t =>
                t.FullName.ToLowerInvariant().Contains(q) ||
                (t.PhoneNumber != null && t.PhoneNumber.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                (t.CCCD != null && t.CCCD.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                (t.Email != null && t.Email.ToLowerInvariant().Contains(q)) ||
                (t.Gender != null && t.Gender.ToLowerInvariant().Contains(q)) ||
                (t.Occupation != null && t.Occupation.ToLowerInvariant().Contains(q)) ||
                (t.Workplace != null && t.Workplace.ToLowerInvariant().Contains(q))
            ).ToList();
        }

        var dtos = list.Select(MapToListDto).ToList();

        if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "all", StringComparison.OrdinalIgnoreCase))
            dtos = dtos.Where(t => string.Equals(t.Status, status, StringComparison.OrdinalIgnoreCase)).ToList();

        if (buildingId.HasValue)
            dtos = dtos.Where(t => t.BuildingId == buildingId.Value).ToList();

        return dtos; 
    }

    public async Task<TenantDetailDto?> GetByIdAsync(int id, int? ownerUserId = null)
    {
        var tenant = await _tenants.GetWithContractsAndRoomsByIdAsync(id, ownerUserId);
        if (tenant == null) return null;

        var dto = MapToDetailDto(tenant);
        dto.History = await GetHistoryInternalAsync(id);
        return dto;
    }

    public async Task<TenantDetailDto> CreateAsync(CreateTenantDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName))
            throw new InvalidOperationException("Họ tên không được để trống.");

        var email = ResolveEmail(dto.Email, dto.PhoneNumber);
        var phone = dto.PhoneNumber?.Trim();

        if (await _tenants.IsEmailOrPhoneTakenAsync(email, phone, excludeTenantId: null))
            throw new InvalidOperationException("Email hoặc số điện thoại đã được sử dụng.");

        var tenant = new Tenant
        {
            FullName = dto.FullName.Trim(),
            Email = email,
            PhoneNumber = phone,
            CCCD = dto.Cccd?.Trim(),
            Address = dto.Address?.Trim(),
            DateOfBirth = dto.DateOfBirth,
            Gender = dto.Gender?.Trim(),
            Occupation = dto.Occupation?.Trim(),
            Workplace = dto.Workplace?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _tenants.Add(tenant);
        await _tenants.SaveChangesAsync();

        if (dto.RoomId.HasValue)
            await CreateOrUpdateActiveContractAsync(tenant.TenantId, dto.RoomId.Value, dto);

        return (await GetByIdAsync(tenant.TenantId))!;
    }

    public async Task<TenantDetailDto> UpdateAsync(int id, UpdateTenantDto dto)
    {
        var tenant = await _tenants.GetTrackedWithContractsByIdAsync(id)
            ?? throw new KeyNotFoundException("Không tìm thấy khách thuê.");

        if (string.IsNullOrWhiteSpace(dto.FullName))
            throw new InvalidOperationException("Họ tên không được để trống.");

        var email = ResolveEmail(dto.Email, dto.PhoneNumber);
        var phone = dto.PhoneNumber?.Trim();

        if (await _tenants.IsEmailOrPhoneTakenAsync(email, phone, excludeTenantId: id))
            throw new InvalidOperationException("Email hoặc số điện thoại đã được sử dụng.");

        tenant.FullName = dto.FullName.Trim();
        tenant.Email = email;
        tenant.PhoneNumber = phone;
        tenant.CCCD = dto.Cccd?.Trim();
        tenant.Address = dto.Address?.Trim();
        tenant.DateOfBirth = dto.DateOfBirth;
        tenant.Gender = dto.Gender?.Trim();
        tenant.Occupation = dto.Occupation?.Trim();
        tenant.Workplace = dto.Workplace?.Trim();
        tenant.IsActive = dto.IsActive;
        tenant.UpdatedAt = DateTime.UtcNow;

        if (dto.RoomId.HasValue)
            await CreateOrUpdateActiveContractAsync(id, dto.RoomId.Value, dto);
        else if (string.Equals(dto.Status, "moved_out", StringComparison.OrdinalIgnoreCase))
        {
            var active = tenant.Contracts.FirstOrDefault(c => c.Status == "Active");
            if (active != null)
            {
                active.Status = "Terminated";
                active.EndDate = DateTime.UtcNow.Date;
                active.Note = dto.Notes ?? active.Note;
            }
        }

        await _tenants.SaveChangesAsync();
        return (await GetByIdAsync(id))!;
    }

    public async Task DeleteAsync(int id)
    {
        var tenant = await _tenants.GetTrackedWithContractsByIdAsync(id)
            ?? throw new KeyNotFoundException("Không tìm thấy khách thuê.");

        if (tenant.Contracts.Count > 0)
            _tenants.RemoveContracts(tenant.Contracts);

        _tenants.Remove(tenant);
        await _tenants.SaveChangesAsync();
    }

    public async Task<string> UploadIdCardAsync(int id, IFormFile file)
    {
        var tenant = await _tenants.GetTrackedByIdAsync(id)
            ?? throw new KeyNotFoundException("Không tìm thấy khách thuê.");

        if (file == null || file.Length == 0)
            throw new InvalidOperationException("File không hợp lệ.");

        if (file.Length > 5 * 1024 * 1024)
            throw new InvalidOperationException("Ảnh tối đa 5MB.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp"))
            throw new InvalidOperationException("Chỉ chấp nhận JPG, PNG, WEBP.");

        var fileName = $"{id}_{Guid.NewGuid():N}{ext}";
        tenant.CCCDImage = await _fileStorage.UploadFormFileAsync(file, "cccd", fileName);
        tenant.UpdatedAt = DateTime.UtcNow;
        await _tenants.SaveChangesAsync();

        return tenant.CCCDImage!;
    }

    public async Task<string> UploadAvatarAsync(int id, IFormFile file)
    {
        var tenant = await _tenants.GetTrackedByIdAsync(id)
            ?? throw new KeyNotFoundException("Không tìm thấy khách thuê.");

        if (file == null || file.Length == 0)
            throw new InvalidOperationException("File không hợp lệ.");

        if (file.Length > 5 * 1024 * 1024)
            throw new InvalidOperationException("Ảnh tối đa 5MB.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".jpg" or ".jpeg" or ".png"))
            throw new InvalidOperationException("Chỉ chấp nhận JPG, PNG.");

        if (!string.IsNullOrEmpty(tenant.Avatar))
            await _fileStorage.DeleteAsync(tenant.Avatar);

        var fileName = $"{id}_{Guid.NewGuid():N}{ext}";
        tenant.Avatar = await _fileStorage.UploadFormFileAsync(file, "avatars", fileName);
        tenant.UpdatedAt = DateTime.UtcNow;
        await _tenants.SaveChangesAsync();

        return tenant.Avatar;
    }

    public async Task DeleteIdCardAsync(int id)
    {
        var tenant = await _tenants.GetTrackedByIdAsync(id)
            ?? throw new KeyNotFoundException("Không tìm thấy khách thuê.");

        if (!string.IsNullOrEmpty(tenant.CCCDImage))
        {
            await _fileStorage.DeleteAsync(tenant.CCCDImage);
            tenant.CCCDImage = null;
            tenant.UpdatedAt = DateTime.UtcNow;
            await _tenants.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<TenantHistoryDto>> GetHistoryAsync(int id)
    {
        if (await _tenants.GetWithContractsAndRoomsByIdAsync(id) == null)
            throw new KeyNotFoundException("Không tìm thấy khách thuê.");
        return await GetHistoryInternalAsync(id);
    }

    private async Task CreateOrUpdateActiveContractAsync(int tenantId, int roomId, CreateTenantDto dto)
    {
        var room = await _tenants.GetRoomAsync(roomId)
            ?? throw new KeyNotFoundException("Không tìm thấy phòng.");

        var existing = await _tenants.FindActiveContractByTenantAndRoomAsync(tenantId, roomId);

        var start = dto.MoveInDate?.Date ?? DateTime.UtcNow.Date;
        var end = dto.MoveOutDate?.Date ?? start.AddYears(1);

        if (existing != null)
        {
            existing.StartDate = start;
            existing.EndDate = end;
            existing.Deposit = dto.Deposit;
            existing.Note = dto.Notes;
        }
        else
        {
            var otherActive = await _tenants.GetActiveContractsForTenantAsync(tenantId);
            foreach (var c in otherActive)
            {
                c.Status = "Terminated";
                c.EndDate = DateTime.UtcNow.Date;
            }

            _tenants.AddContract(new Contract
            {
                TenantId = tenantId,
                RoomId = roomId,
                StartDate = start,
                EndDate = end,
                Deposit = dto.Deposit,
                Note = dto.Notes,
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            });
        }

        if (!string.Equals(room.Status, "Occupied", StringComparison.OrdinalIgnoreCase))
            room.Status = "Occupied";

        await _tenants.SaveChangesAsync();
    }

    private async Task<List<TenantHistoryDto>> GetHistoryInternalAsync(int tenantId)
    {
        var contracts = await _tenants.GetContractHistoryForTenantAsync(tenantId);
        return contracts.Select(c => new TenantHistoryDto
        {
            ContractId = c.ContractId,
            RoomId = c.RoomId,
            RoomNumber = c.Room?.RoomName ?? string.Empty,
            BuildingId = c.Room?.BuildingId,
            BuildingName = c.Room?.Building?.BuildingName,
            StartDate = c.StartDate,
            EndDate = c.EndDate,
            Deposit = c.Deposit,
            Status = c.Status,
            Notes = c.Note,
            CreatedAt = c.CreatedAt
        }).ToList();
    }

    private static string? ResolveEmail(string? email, string? phone)
    {
        if (!string.IsNullOrWhiteSpace(email)) return email.Trim();
        if (!string.IsNullOrWhiteSpace(phone))
        {
            var digits = new string(phone.Where(char.IsDigit).ToArray());
            return $"tenant_{digits}@rental.local";
        }
        return null;
    }

    private static TenantListDto MapToListDto(Tenant tenant)
    {
        var active = tenant.Contracts
            .Where(c => c.Status == "Active")
            .OrderByDescending(c => c.StartDate)
            .FirstOrDefault();
        var latest = tenant.Contracts.OrderByDescending(c => c.CreatedAt).FirstOrDefault();

        return new TenantListDto
        {
            Id = tenant.TenantId,
            FullName = tenant.FullName,
            PhoneNumber = tenant.PhoneNumber,
            Email = tenant.Email,
            Cccd = tenant.CCCD,
            IdCardImage = tenant.CCCDImage,
            Avatar = tenant.Avatar,
            Address = tenant.Address,
            DateOfBirth = tenant.DateOfBirth,
            Gender = tenant.Gender,
            Occupation = tenant.Occupation,
            Workplace = tenant.Workplace,
            IsActive = tenant.IsActive,
            Status = MapStatus(tenant, active, latest),
            RoomId = active?.RoomId,
            RoomNumber = active?.Room?.RoomName,
            BuildingId = active?.Room?.BuildingId,
            BuildingName = active?.Room?.Building?.BuildingName,
            ContractId = active?.ContractId,
            MoveInDate = active?.StartDate,
            MoveOutDate = active?.Status == "Terminated" ? active.EndDate : null,
            Deposit = active?.Deposit ?? 0,
            Notes = active?.Note,
            CreatedAt = tenant.CreatedAt
        };
    }

    private static TenantDetailDto MapToDetailDto(Tenant tenant)
    {
        var list = MapToListDto(tenant);
        return new TenantDetailDto
        {
            Id = list.Id,
            FullName = list.FullName,
            PhoneNumber = list.PhoneNumber,
            Email = list.Email,
            Cccd = list.Cccd,
            IdCardImage = list.IdCardImage,
            Avatar = list.Avatar,
            Address = list.Address,
            DateOfBirth = list.DateOfBirth,
            Gender = list.Gender,
            Occupation = list.Occupation,
            Workplace = list.Workplace,
            IsActive = list.IsActive,
            Status = list.Status,
            RoomId = list.RoomId,
            RoomNumber = list.RoomNumber,
            BuildingId = list.BuildingId,
            BuildingName = list.BuildingName,
            ContractId = list.ContractId,
            MoveInDate = list.MoveInDate,
            MoveOutDate = list.MoveOutDate,
            Deposit = list.Deposit,
            Notes = list.Notes,
            CreatedAt = list.CreatedAt
        };
    }

    private static string MapStatus(Tenant tenant, Contract? active, Contract? latest)
    {
        if (active != null) return "active";
        if (latest != null && latest.Status == "Terminated") return "moved_out";
        if (!tenant.IsActive) return "inactive";
        return "inactive";
    }
}
