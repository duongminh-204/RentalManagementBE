using Backend.DTOs.Contracts;
using Backend.Entities;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using System.Text;
using System.Text.Json;

namespace Backend.Services;

public class ContractService : IContractService
{
    private readonly IContractRepository _contracts;
    private readonly IWebHostEnvironment _env;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public ContractService(IContractRepository contracts, IWebHostEnvironment env)
    {
        _contracts = contracts;
        _env = env;
    }

    public async Task<IEnumerable<ContractDto>> GetAllAsync(
        int? roomId = null,
        int? tenantId = null,
        string? search = null,
        string? statusFilter = null,
        string? sortBy = null,
        bool sortDesc = true)
    {
        var list = await _contracts.ListAsync(roomId, tenantId, search, statusFilter, sortBy, sortDesc);
        return list.Select(MapToDto);
    }

    public async Task<ContractDto?> GetByIdAsync(int id)
    {
        var contract = await _contracts.GetByIdAsync(id);
        return contract != null ? MapToDto(contract) : null;
    }

    public async Task<ContractDetailDto?> GetDetailAsync(int id)
    {
        var contract = await _contracts.GetByIdAsync(id);
        if (contract == null) return null;

        var dto = MapToDetailDto(contract);
        var invoices = await _contracts.GetInvoicesWithPaymentsByRoomAsync(contract.RoomId);
        dto.PaymentHistory = invoices.Select(MapPaymentHistory).ToList();
        return dto;
    }

    public async Task<IEnumerable<ContractDto>> GetExpiringAsync(int days)
    {
        var list = await _contracts.GetExpiringAsync(days);
        return list.Select(MapToDto);
    }

    public async Task<IEnumerable<ContractReminderDto>> GetRemindersAsync()
    {
        var reminders = new List<ContractReminderDto>();
        var today = DateTime.UtcNow.Date;

        foreach (var days in new[] { 7, 3, 1 })
        {
            var expiring = await _contracts.GetExpiringAsync(days);
            foreach (var c in expiring)
            {
                var remaining = (c.EndDate.Date - today).Days;
                if (remaining is 7 or 3 or 1)
                {
                    reminders.Add(new ContractReminderDto
                    {
                        ContractId = c.ContractId,
                        TenantName = c.Tenant?.FullName ?? "",
                        RoomName = c.Room?.RoomName ?? "",
                        EndDate = c.EndDate,
                        DaysRemaining = remaining,
                        ReminderType = $"expires_in_{remaining}_days"
                    });
                }
            }
        }

        var expired = await _contracts.GetExpiredNotRenewedAsync();
        foreach (var c in expired)
        {
            reminders.Add(new ContractReminderDto
            {
                ContractId = c.ContractId,
                TenantName = c.Tenant?.FullName ?? "",
                RoomName = c.Room?.RoomName ?? "",
                EndDate = c.EndDate,
                DaysRemaining = (c.EndDate.Date - today).Days,
                ReminderType = "expired_not_renewed"
            });
        }

        return reminders
            .GroupBy(r => $"{r.ContractId}:{r.ReminderType}")
            .Select(g => g.First())
            .OrderBy(r => r.DaysRemaining);
    }

    public async Task<ContractDto> CreateAsync(CreateContractDto dto)
    {
        await ValidateContractPartiesAsync(dto.TenantId, dto.RoomId);

        var dbStatus = MapStatusToDb(dto.Status);
        if (string.Equals(dbStatus, "Active", StringComparison.OrdinalIgnoreCase) &&
            await _contracts.RoomHasActiveContractAsync(dto.RoomId))
            throw new InvalidOperationException("Phòng đã có hợp đồng đang hiệu lực.");

        var room = await _contracts.GetRoomAsync(dto.RoomId);
        var rentPrice = dto.RentPrice > 0 ? dto.RentPrice : room?.Price ?? 0;

        var contract = new Contract
        {
            TenantId = dto.TenantId,
            RoomId = dto.RoomId,
            StartDate = dto.StartDate.Date,
            EndDate = dto.EndDate.Date,
            RentPrice = rentPrice,
            Deposit = dto.Deposit ?? 0,
            PaymentCycle = NormalizePaymentCycle(dto.PaymentCycle),
            DepositStatus = "Holding",
            Status = dbStatus,
            Note = BuildNote(dto.Terms, dto.Notes),
            DepositHistory = SerializeDepositHistory([
                new DepositHistoryItemDto
                {
                    ChangedAt = DateTime.UtcNow,
                    FromStatus = "",
                    ToStatus = "Holding",
                    Amount = dto.Deposit ?? 0,
                    Note = "Tiền cọc khi tạo hợp đồng"
                }
            ]),
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
        contract.RentPrice = dto.RentPrice;
        contract.Deposit = dto.Deposit ?? contract.Deposit;
        contract.PaymentCycle = NormalizePaymentCycle(dto.PaymentCycle);
        contract.Status = newStatus;
        contract.Note = BuildNote(dto.Terms, dto.Notes);

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
        var oldContract = await _contracts.GetTrackedByIdAsync(id)
            ?? throw new KeyNotFoundException("Không tìm thấy hợp đồng.");

        var extendMonths = dto.ExtendMonths > 0 ? dto.ExtendMonths : 12;
        var newRentPrice = dto.NewRentPrice ?? oldContract.RentPrice;
        var oldEndDate = oldContract.EndDate;
        var newEndDate = oldEndDate.AddMonths(extendMonths);

        var historyItem = new RenewalHistoryItemDto
        {
            FromContractId = oldContract.ContractId,
            RenewedAt = DateTime.UtcNow,
            OldEndDate = oldEndDate,
            NewEndDate = newEndDate,
            OldRentPrice = oldContract.RentPrice,
            NewRentPrice = newRentPrice,
            ExtendMonths = extendMonths,
            Notes = dto.Notes
        };

        if (dto.CloneContract)
        {
            oldContract.Status = "Terminated";
            oldContract.TerminatedAt = DateTime.UtcNow;
            oldContract.TerminationReason = "Gia hạn - chuyển sang hợp đồng mới";

            var (terms, notes) = ParseNote(oldContract.Note);
            var newContract = new Contract
            {
                TenantId = oldContract.TenantId,
                RoomId = oldContract.RoomId,
                ParentContractId = oldContract.ContractId,
                StartDate = oldEndDate.AddDays(1),
                EndDate = newEndDate,
                RentPrice = newRentPrice,
                Deposit = oldContract.Deposit,
                PaymentCycle = oldContract.PaymentCycle,
                DepositStatus = oldContract.DepositStatus,
                Status = "Active",
                Note = BuildNote(terms, dto.Notes ?? notes),
                ContractFile = oldContract.ContractFile,
                RenewalHistory = SerializeRenewalHistory(
                    DeserializeRenewalHistory(oldContract.RenewalHistory).Append(historyItem).ToList()),
                DepositHistory = oldContract.DepositHistory,
                CreatedAt = DateTime.UtcNow
            };

            historyItem.ToContractId = null;
            _contracts.Add(newContract);
            await _contracts.SaveChangesAsync();
            historyItem.ToContractId = newContract.ContractId;

            var renewalList = DeserializeRenewalHistory(newContract.RenewalHistory);
            if (renewalList.Count > 0)
            {
                renewalList[^1] = historyItem;
                newContract.RenewalHistory = SerializeRenewalHistory(renewalList);
            }

            await _contracts.SaveChangesAsync();
            await UpdateRoomOccupancyAsync(oldContract.RoomId);

            var created = await _contracts.GetByIdAsync(newContract.ContractId);
            return MapToDto(created!);
        }

        oldContract.EndDate = newEndDate;
        oldContract.RentPrice = newRentPrice;
        oldContract.Status = "Active";

        var existingHistory = DeserializeRenewalHistory(oldContract.RenewalHistory);
        existingHistory.Add(historyItem);
        oldContract.RenewalHistory = SerializeRenewalHistory(existingHistory);

        if (!string.IsNullOrWhiteSpace(dto.Notes))
            oldContract.Note = AppendNote(oldContract.Note, dto.Notes);

        await _contracts.SaveChangesAsync();
        await UpdateRoomOccupancyAsync(oldContract.RoomId);

        var updated = await _contracts.GetByIdAsync(id);
        return MapToDto(updated!);
    }

    public async Task<ContractDto> TerminateAsync(int id, TerminateContractDto dto)
    {
        var contract = await _contracts.GetTrackedByIdAsync(id)
            ?? throw new KeyNotFoundException("Không tìm thấy hợp đồng.");

        if (string.Equals(contract.Status, "Terminated", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(contract.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Hợp đồng đã được chấm dứt.");

        var deduction = Math.Max(0, dto.DepositDeductionAmount);
        if (deduction > contract.Deposit)
            throw new InvalidOperationException("Số tiền khấu trừ không được lớn hơn tiền cọc.");

        var refund = contract.Deposit - deduction;

        contract.Status = "Terminated";
        contract.TerminatedAt = DateTime.UtcNow;
        contract.TerminationReason = dto.Reason;
        contract.DepositDeductionAmount = deduction;
        contract.DepositRefundAmount = refund;
        contract.DepositStatus = deduction > 0 && refund > 0
            ? "Deducted"
            : deduction >= contract.Deposit
                ? "Deducted"
                : "Refunded";

        var depositHistory = DeserializeDepositHistory(contract.DepositHistory);
        depositHistory.Add(new DepositHistoryItemDto
        {
            ChangedAt = DateTime.UtcNow,
            FromStatus = "Holding",
            ToStatus = contract.DepositStatus,
            Amount = refund,
            Note = $"Chấm dứt HĐ. Khấu trừ: {deduction:N0}đ. Hoàn: {refund:N0}đ. {dto.Notes}"
        });
        contract.DepositHistory = SerializeDepositHistory(depositHistory);

        if (!string.IsNullOrWhiteSpace(dto.Notes))
            contract.Note = AppendNote(contract.Note, dto.Notes);

        var tenant = await _contracts.GetTenantAsync(contract.TenantId);
        if (tenant != null)
        {
            tenant.MoveOutDate = DateTime.UtcNow;
            tenant.IsActive = false;
        }

        await _contracts.SaveChangesAsync();
        await UpdateRoomOccupancyAsync(contract.RoomId);

        var updated = await _contracts.GetByIdAsync(id);
        return MapToDto(updated!);
    }

    public async Task<ContractDto> UpdateDepositAsync(int id, UpdateDepositDto dto)
    {
        var contract = await _contracts.GetTrackedByIdAsync(id)
            ?? throw new KeyNotFoundException("Không tìm thấy hợp đồng.");

        var newStatus = NormalizeDepositStatus(dto.DepositStatus);
        var oldStatus = contract.DepositStatus;

        contract.DepositStatus = newStatus;
        if (dto.RefundAmount.HasValue)
            contract.DepositRefundAmount = dto.RefundAmount.Value;
        if (dto.DeductionAmount.HasValue)
            contract.DepositDeductionAmount = dto.DeductionAmount.Value;

        var depositHistory = DeserializeDepositHistory(contract.DepositHistory);
        depositHistory.Add(new DepositHistoryItemDto
        {
            ChangedAt = DateTime.UtcNow,
            FromStatus = oldStatus,
            ToStatus = newStatus,
            Amount = dto.RefundAmount ?? dto.DeductionAmount ?? contract.Deposit,
            Note = dto.Note
        });
        contract.DepositHistory = SerializeDepositHistory(depositHistory);

        await _contracts.SaveChangesAsync();

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

    public async Task<string> GenerateFromTemplateAsync(int id, GenerateContractDto? dto = null)
    {
        var contract = await _contracts.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Không tìm thấy hợp đồng.");

        var templateName = dto?.TemplateName ?? "default";
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var templatesDir = Path.Combine(webRoot, "uploads", "templates");
        Directory.CreateDirectory(templatesDir);

        var templatePath = Path.Combine(templatesDir, $"{templateName}.txt");
        if (!File.Exists(templatePath))
        {
            await File.WriteAllTextAsync(templatePath, DefaultContractTemplate, Encoding.UTF8);
        }

        var template = await File.ReadAllTextAsync(templatePath, Encoding.UTF8);
        var (terms, notes) = ParseNote(contract.Note);

        var content = template
            .Replace("{{TENANT_NAME}}", contract.Tenant?.FullName ?? "")
            .Replace("{{TENANT_PHONE}}", contract.Tenant?.PhoneNumber ?? "")
            .Replace("{{TENANT_CCCD}}", contract.Tenant?.CCCD ?? "")
            .Replace("{{ROOM_NAME}}", contract.Room?.RoomName ?? "")
            .Replace("{{START_DATE}}", contract.StartDate.ToString("dd/MM/yyyy"))
            .Replace("{{END_DATE}}", contract.EndDate.ToString("dd/MM/yyyy"))
            .Replace("{{RENT_PRICE}}", contract.RentPrice.ToString("N0"))
            .Replace("{{DEPOSIT}}", contract.Deposit.ToString("N0"))
            .Replace("{{PAYMENT_CYCLE}}", contract.PaymentCycle)
            .Replace("{{TERMS}}", terms ?? "")
            .Replace("{{NOTES}}", notes ?? "");

        var uploadsDir = Path.Combine(webRoot, "uploads", "contracts");
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"{id}_generated_{Guid.NewGuid():N}.txt";
        var filePath = Path.Combine(uploadsDir, fileName);
        await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);

        var tracked = await _contracts.GetTrackedByIdAsync(id)
            ?? throw new KeyNotFoundException("Không tìm thấy hợp đồng.");
        tracked.ContractFile = $"/uploads/contracts/{fileName}";
        await _contracts.SaveChangesAsync();

        return tracked.ContractFile;
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
            "terminated" => "Terminated",
            "cancelled" => "Cancelled",
            "active" or "expiring_soon" or "expired" => "Active",
            _ => "Active"
        };
    }

    private static string NormalizePaymentCycle(string? cycle) =>
        cycle?.Trim().ToLowerInvariant() switch
        {
            "quarterly" => "Quarterly",
            "flexible" => "Flexible",
            _ => "Monthly"
        };

    private static string NormalizeDepositStatus(string? status) =>
        status?.Trim().ToLowerInvariant() switch
        {
            "refunded" => "Refunded",
            "deducted" => "Deducted",
            _ => "Holding"
        };

    private static string? BuildNote(string? terms, string? notes)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(terms))
            parts.Add(terms.Trim());
        if (!string.IsNullOrWhiteSpace(notes))
            parts.Add(notes.Trim());
        return parts.Count > 0 ? string.Join("\n\n", parts) : null;
    }

    private static string? AppendNote(string? existing, string addition) =>
        string.IsNullOrWhiteSpace(existing) ? addition : $"{existing}\n\n{addition}";

    private static (string? terms, string? notes) ParseNote(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
            return (null, null);

        var parts = note.Split("\n\n", 2, StringSplitOptions.None);
        return parts.Length switch
        {
            1 => (parts[0].Trim(), null),
            _ => (parts[0].Trim(), parts[1].Trim())
        };
    }

    private static List<RenewalHistoryItemDto> DeserializeRenewalHistory(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<RenewalHistoryItemDto>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static string SerializeRenewalHistory(List<RenewalHistoryItemDto> items) =>
        JsonSerializer.Serialize(items, JsonOptions);

    private static List<DepositHistoryItemDto> DeserializeDepositHistory(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<DepositHistoryItemDto>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static string SerializeDepositHistory(List<DepositHistoryItemDto> items) =>
        JsonSerializer.Serialize(items, JsonOptions);

    private static PaymentHistoryItemDto MapPaymentHistory(Invoice invoice) =>
        new()
        {
            InvoiceId = invoice.InvoiceId,
            MonthYear = invoice.MonthYear,
            TotalAmount = invoice.TotalAmount,
            Status = invoice.Status,
            PaymentDate = invoice.PaymentDate,
            PaidAmount = invoice.Payments?.Where(p => p.Status == "Success").Sum(p => p.Amount) ?? 0
        };

    private static ContractDto MapToDto(Contract contract)
    {
        var (terms, notes) = ParseNote(contract.Note);
        var dbStatus = contract.Status ?? "Active";
        var today = DateTime.UtcNow.Date;

        var status = dbStatus switch
        {
            "Pending" => "pending",
            "Terminated" => "terminated",
            "Cancelled" => "cancelled",
            "Active" when contract.EndDate.Date < today => "expired",
            "Active" when (contract.EndDate.Date - today).TotalDays <= 30 => "expiring_soon",
            _ => "active"
        };

        return new ContractDto
        {
            Id = contract.ContractId,
            TenantId = contract.TenantId,
            RoomId = contract.RoomId,
            ParentContractId = contract.ParentContractId,
            TenantName = contract.Tenant?.FullName,
            RoomName = contract.Room?.RoomName,
            StartDate = contract.StartDate,
            EndDate = contract.EndDate,
            RentPrice = contract.RentPrice,
            Deposit = contract.Deposit,
            PaymentCycle = contract.PaymentCycle,
            DepositStatus = contract.DepositStatus,
            DepositRefundAmount = contract.DepositRefundAmount,
            DepositDeductionAmount = contract.DepositDeductionAmount,
            Terms = terms,
            Notes = notes,
            Status = status,
            FileUrl = contract.ContractFile,
            IsTerminated = string.Equals(dbStatus, "Terminated", StringComparison.OrdinalIgnoreCase),
            TerminationReason = contract.TerminationReason,
            TerminatedAt = contract.TerminatedAt,
            RenewalHistory = DeserializeRenewalHistory(contract.RenewalHistory),
            DepositHistory = DeserializeDepositHistory(contract.DepositHistory)
        };
    }

    private static ContractDetailDto MapToDetailDto(Contract contract)
    {
        var dto = MapToDto(contract);
        return new ContractDetailDto
        {
            Id = dto.Id,
            TenantId = dto.TenantId,
            RoomId = dto.RoomId,
            ParentContractId = dto.ParentContractId,
            TenantName = dto.TenantName,
            RoomName = dto.RoomName,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            RentPrice = dto.RentPrice,
            Deposit = dto.Deposit,
            PaymentCycle = dto.PaymentCycle,
            DepositStatus = dto.DepositStatus,
            DepositRefundAmount = dto.DepositRefundAmount,
            DepositDeductionAmount = dto.DepositDeductionAmount,
            Terms = dto.Terms,
            Notes = dto.Notes,
            Status = dto.Status,
            FileUrl = dto.FileUrl,
            IsTerminated = dto.IsTerminated,
            TerminationReason = dto.TerminationReason,
            TerminatedAt = dto.TerminatedAt,
            RenewalHistory = dto.RenewalHistory,
            DepositHistory = dto.DepositHistory,
            Tenant = contract.Tenant == null ? null : new TenantSummaryDto
            {
                Id = contract.Tenant.TenantId,
                FullName = contract.Tenant.FullName,
                PhoneNumber = contract.Tenant.PhoneNumber,
                Email = contract.Tenant.Email,
                CCCD = contract.Tenant.CCCD,
                Address = contract.Tenant.Address
            },
            Room = contract.Room == null ? null : new RoomSummaryDto
            {
                Id = contract.Room.RoomId,
                RoomName = contract.Room.RoomName,
                Status = contract.Room.Status,
                Price = contract.Room.Price,
                Area = contract.Room.Area
            }
        };
    }

    private const string DefaultContractTemplate = """
        HỢP ĐỒNG THUÊ PHÒNG TRỌ

        Bên cho thuê và Bên thuê thống nhất ký hợp đồng với các điều khoản sau:

        1. Bên thuê: {{TENANT_NAME}} - CCCD: {{TENANT_CCCD}} - SĐT: {{TENANT_PHONE}}
        2. Phòng thuê: {{ROOM_NAME}}
        3. Thời hạn: từ {{START_DATE}} đến {{END_DATE}}
        4. Giá thuê: {{RENT_PRICE}} VNĐ / {{PAYMENT_CYCLE}}
        5. Tiền cọc: {{DEPOSIT}} VNĐ

        ĐIỀU KHOẢN:
        {{TERMS}}

        GHI CHÚ:
        {{NOTES}}
        """;
}
