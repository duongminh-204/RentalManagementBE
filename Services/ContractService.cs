using Backend.DTOs.Contracts;
using Backend.Entities;
using Backend.Interfaces;
using Backend.Repositories.Interfaces;

namespace Backend.Services;

public class ContractService : IContractService
{
    private readonly IContractRepository _contracts;
    private readonly IWebHostEnvironment _env;

    public ContractService(IContractRepository contracts, IWebHostEnvironment env)
    {
        _contracts = contracts;
        _env = env;
    }

    public async Task<IEnumerable<ContractDto>> GetAllAsync(int? roomId = null, int? tenantId = null)
    {
        var list = await _contracts.ListAsync(roomId, tenantId);
        return list.Select(MapToDto);
    }

    public async Task<ContractDto?> GetByIdAsync(int id)
    {
        var contract = await _contracts.GetByIdAsync(id);
        return contract != null ? MapToDto(contract) : null;
    }

    public async Task<ContractDto> CreateAsync(CreateContractDto dto)
    {
        await ValidateContractPartiesAsync(dto.TenantId, dto.RoomId);

        if (string.Equals(MapStatusToDb(dto.Status), "Active", StringComparison.OrdinalIgnoreCase) &&
            await _contracts.RoomHasActiveContractAsync(dto.RoomId))
            throw new InvalidOperationException("Phòng đã có hợp đồng đang hiệu lực.");

        var contract = new Contract
        {
            TenantId = dto.TenantId,
            RoomId = dto.RoomId,
            StartDate = dto.StartDate.Date,
            EndDate = dto.EndDate.Date,
            Deposit = 0,
            Status = MapStatusToDb(dto.Status),
            Note = BuildNote(dto.ContractNumber, dto.Terms, dto.Notes),
            CreatedAt = DateTime.UtcNow
        };

        _contracts.Add(contract);
        await UpdateRoomOccupancyAsync(dto.RoomId);
        await _contracts.SaveChangesAsync();

        var created = await _contracts.GetByIdAsync(contract.ContractId);
        return MapToDto(created!);
    }

    public async Task<ContractDto> UpdateAsync(int id, UpdateContractDto dto)
    {
        var contract = await _contracts.GetTrackedByIdAsync(id)
            ?? throw new KeyNotFoundException("Không tìm thấy hợp đồng.");

        await ValidateContractPartiesAsync(dto.TenantId, dto.RoomId);

        var newStatus = MapStatusToDb(dto.Status);
        if (string.Equals(newStatus, "Active", StringComparison.OrdinalIgnoreCase) &&
            await _contracts.RoomHasActiveContractAsync(dto.RoomId, id))
            throw new InvalidOperationException("Phòng đã có hợp đồng đang hiệu lực khác.");

        contract.TenantId = dto.TenantId;
        contract.RoomId = dto.RoomId;
        contract.StartDate = dto.StartDate.Date;
        contract.EndDate = dto.EndDate.Date;
        contract.Status = newStatus;
        contract.Note = BuildNote(dto.ContractNumber, dto.Terms, dto.Notes);

        await _contracts.SaveChangesAsync();
        await UpdateRoomOccupancyAsync(contract.RoomId);

        var updated = await _contracts.GetByIdAsync(id);
        return MapToDto(updated!);
    }

    public async Task DeleteAsync(int id)
    {
        var contract = await _contracts.GetTrackedByIdAsync(id)
            ?? throw new KeyNotFoundException("Không tìm thấy hợp đồng.");

        var roomId = contract.RoomId;
        _contracts.Remove(contract);
        await _contracts.SaveChangesAsync();
        await UpdateRoomOccupancyAsync(roomId);
    }

    public async Task<ContractDto> RenewAsync(int id, RenewContractDto dto)
    {
        var contract = await _contracts.GetTrackedByIdAsync(id)
            ?? throw new KeyNotFoundException("Không tìm thấy hợp đồng.");

        contract.EndDate = dto.NewEndDate.Date;
        contract.Status = "Active";
        if (!string.IsNullOrWhiteSpace(dto.Notes))
            contract.Note = AppendNote(contract.Note, dto.Notes);

        await _contracts.SaveChangesAsync();
        await UpdateRoomOccupancyAsync(contract.RoomId);

        var updated = await _contracts.GetByIdAsync(id);
        return MapToDto(updated!);
    }

    public async Task<string> UploadFileAsync(int id, IFormFile file)
    {
        var contract = await _contracts.GetTrackedByIdAsync(id)
            ?? throw new KeyNotFoundException("Không tìm thấy hợp đồng.");

        if (file == null || file.Length == 0)
            throw new InvalidOperationException("File không hợp lệ.");

        var uploadsDir = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "uploads", "contracts");
        Directory.CreateDirectory(uploadsDir);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".pdf" or ".jpg" or ".jpeg" or ".png"))
            throw new InvalidOperationException("Chỉ chấp nhận PDF hoặc ảnh.");

        var fileName = $"{id}_{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        contract.ContractFile = $"/uploads/contracts/{fileName}";
        await _contracts.SaveChangesAsync();
        return contract.ContractFile;
    }

    private async Task ValidateContractPartiesAsync(int tenantId, int roomId)
    {
        if (await _contracts.GetTenantAsync(tenantId) == null)
            throw new KeyNotFoundException("Không tìm thấy khách thuê.");
        if (await _contracts.GetRoomAsync(roomId) == null)
            throw new KeyNotFoundException("Không tìm thấy phòng.");
    }

    private async Task UpdateRoomOccupancyAsync(int roomId)
    {
        var room = await _contracts.GetRoomAsync(roomId);
        if (room == null) return;

        var hasActive = await _contracts.RoomHasActiveContractAsync(roomId);
        room.Status = hasActive ? "Occupied" : "Available";
        await _contracts.SaveChangesAsync();
    }

    private static string MapStatusToDb(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return "Active";
        return status.ToLowerInvariant() switch
        {
            "pending" => "Pending",
            "terminated" or "expired" => "Terminated",
            "active" or "expiring_soon" => "Active",
            _ => "Active"
        };
    }

    private static string? BuildNote(string? contractNumber, string? terms, string? notes)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(contractNumber))
            parts.Add($"[HD:{contractNumber.Trim()}]");
        if (!string.IsNullOrWhiteSpace(terms))
            parts.Add(terms.Trim());
        if (!string.IsNullOrWhiteSpace(notes))
            parts.Add(notes.Trim());
        return parts.Count > 0 ? string.Join("\n", parts) : null;
    }

    private static string? AppendNote(string? existing, string addition) =>
        string.IsNullOrWhiteSpace(existing) ? addition : $"{existing}\n{addition}";

    private static ContractDto MapToDto(Contract contract)
    {
        var (contractNumber, terms, notes) = ParseNote(contract.Note);
        var dbStatus = contract.Status ?? "Active";
        var status = dbStatus switch
        {
            "Pending" => "pending",
            "Terminated" => "terminated",
            "Active" when contract.EndDate.Date < DateTime.UtcNow.Date => "expired",
            "Active" when (contract.EndDate.Date - DateTime.UtcNow.Date).TotalDays <= 30 => "expiring_soon",
            _ => "active"
        };

        return new ContractDto
        {
            Id = contract.ContractId,
            ContractNumber = contractNumber ?? $"HD-{contract.ContractId:D5}",
            TenantId = contract.TenantId,
            RoomId = contract.RoomId,
            StartDate = contract.StartDate,
            EndDate = contract.EndDate,
            RentalPrice = contract.Room?.Price ?? 0,
            Terms = terms,
            Notes = notes,
            Status = status,
            FileUrl = contract.ContractFile,
            IsTerminated = string.Equals(dbStatus, "Terminated", StringComparison.OrdinalIgnoreCase)
        };
    }

    private static (string? contractNumber, string? terms, string? notes) ParseNote(string? note)
    {
        if (string.IsNullOrWhiteSpace(note)) return (null, null, null);

        var lines = note.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        string? number = null;
        var rest = new List<string>();

        foreach (var line in lines)
        {
            if (line.StartsWith("[HD:", StringComparison.OrdinalIgnoreCase) && line.EndsWith(']'))
            {
                number = line[4..^1];
                continue;
            }
            rest.Add(line);
        }

        var terms = rest.Count > 0 ? rest[0] : null;
        var notes = rest.Count > 1 ? string.Join("\n", rest.Skip(1)) : null;
        return (number, terms, notes);
    }
}
