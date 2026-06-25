using Backend.DTOs.Legal;
using Backend.Entities;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;

namespace Backend.Services;

public class LegalService : ILegalService
{
    private readonly ILegalRepository _legal;
    private readonly IFileStorageService _fileStorage;
    private readonly INotificationService _notifications;

    private const int TempResidenceGraceDays = 7;
    private const int DocumentExpiryWarningDays = 30;
    private const int ContractExpiryWarningDays = 30;

    private static readonly Dictionary<string, string> TenantItemLabels = new()
    {
        ["cccd_number"] = "Số CCCD/CMND",
        ["cccd_image"] = "Ảnh giấy tờ tùy thân",
        ["rental_contract"] = "Hợp đồng thuê phòng",
        ["emergency_contact"] = "Liên hệ khẩn cấp",
        ["deposit_receipt"] = "Biên nhận tiền cọc",
        ["temp_residence"] = "Giấy xác nhận tạm trú"
    };

    private static readonly Dictionary<string, string> RoomItemLabels = new()
    {
        ["tenant_info"] = "Thông tin khách thuê",
        ["valid_contract"] = "Hợp đồng còn hiệu lực",
        ["deposit_status"] = "Tình trạng tiền cọc",
        ["temp_residence"] = "Hồ sơ tạm trú",
        ["handover_record"] = "Biên bản bàn giao phòng",
        ["asset_photos"] = "Ảnh hiện trạng tài sản"
    };

    public LegalService(ILegalRepository legal, IFileStorageService fileStorage, INotificationService notifications)
    {
        _legal = legal;
        _fileStorage = fileStorage;
        _notifications = notifications;
    }

    public async Task<LegalDashboardDto> GetDashboardAsync(int ownerUserId, CancellationToken ct = default)
    {
        var rooms = await _legal.GetOwnerRoomsWithDetailsAsync(ownerUserId, ct);
        var tenants = await _legal.GetOwnerTenantsWithDetailsAsync(ownerUserId, ct);
        var documents = await _legal.GetBuildingDocumentsAsync(null, ownerUserId, ct);
        var tenantProfiles = await LoadTenantProfilesAsync(tenants.Select(t => t.TenantId), ct);
        var roomProfiles = await LoadRoomProfilesAsync(rooms.Select(r => r.RoomId), ct);

        var occupiedRooms = rooms.Where(r => HasActiveContract(r)).ToList();
        var roomSummaries = occupiedRooms
            .Select(r => BuildRoomSummary(r, tenantProfiles, roomProfiles))
            .ToList();

        var alerts = await BuildAlertsAsync(rooms, tenants, documents, tenantProfiles, roomProfiles, ct);

        return new LegalDashboardDto
        {
            TotalRooms = rooms.Count,
            OccupiedRooms = occupiedRooms.Count,
            RoomsComplete = roomSummaries.Count(r => r.CompletionPercent >= 100),
            RoomsIncomplete = roomSummaries.Count(r => r.CompletionPercent < 100),
            ExpiringContracts = alerts.Count(a => a.Type == "contract_expiring"),
            PendingTempResidence = alerts.Count(a => a.Type == "temp_residence_pending"),
            ExpiringDocuments = alerts.Count(a => a.Type is "document_expiring" or "document_expired"),
            ActionItemsCount = alerts.Count,
            LegalScore = CalculateLegalScore(rooms, tenants, documents, tenantProfiles, roomProfiles),
            ActionItems = alerts.Take(12).ToList()
        };
    }

    public async Task<List<LegalAlertDto>> GetAlertsAsync(int ownerUserId, CancellationToken ct = default)
    {
        var rooms = await _legal.GetOwnerRoomsWithDetailsAsync(ownerUserId, ct);
        var tenants = await _legal.GetOwnerTenantsWithDetailsAsync(ownerUserId, ct);
        var documents = await _legal.GetBuildingDocumentsAsync(null, ownerUserId, ct);
        var tenantProfiles = await LoadTenantProfilesAsync(tenants.Select(t => t.TenantId), ct);
        var roomProfiles = await LoadRoomProfilesAsync(rooms.Select(r => r.RoomId), ct);
        return await BuildAlertsAsync(rooms, tenants, documents, tenantProfiles, roomProfiles, ct);
    }

    public async Task<List<TenantLegalSummaryDto>> GetTenantSummariesAsync(int ownerUserId, CancellationToken ct = default)
    {
        var tenants = await _legal.GetOwnerTenantsWithDetailsAsync(ownerUserId, ct);
        var profiles = await LoadTenantProfilesAsync(tenants.Select(t => t.TenantId), ct);
        return tenants
            .Select(t => BuildTenantSummary(t, profiles.GetValueOrDefault(t.TenantId)))
            .OrderBy(t => t.CompletionPercent)
            .ThenBy(t => t.FullName)
            .ToList();
    }

    public async Task<TenantLegalDetailDto?> GetTenantDetailAsync(int tenantId, int ownerUserId, CancellationToken ct = default)
    {
        var tenant = await _legal.GetOwnerTenantByIdAsync(tenantId, ownerUserId, ct);
        if (tenant == null) return null;

        var profile = await _legal.GetTenantLegalProfileAsync(tenantId, ct);
        var summary = BuildTenantSummary(tenant, profile);
        var activeContract = GetActiveContract(tenant);

        return new TenantLegalDetailDto
        {
            TenantId = summary.TenantId,
            FullName = summary.FullName,
            RoomName = summary.RoomName,
            RoomId = summary.RoomId,
            CompletionPercent = summary.CompletionPercent,
            CompletedCount = summary.CompletedCount,
            TotalCount = summary.TotalCount,
            TempResidencePending = summary.TempResidencePending,
            Items = summary.Items,
            PhoneNumber = tenant.PhoneNumber,
            CCCD = tenant.CCCD,
            CCCDImage = tenant.CCCDImage,
            MoveInDate = tenant.MoveInDate,
            ActiveContractId = activeContract?.ContractId,
            EmergencyContactName = profile?.EmergencyContactName,
            EmergencyContactPhone = profile?.EmergencyContactPhone,
            EmergencyContactRelation = profile?.EmergencyContactRelation,
            DepositReceiptFile = profile?.DepositReceiptFile,
            TempResidenceFile = profile?.TempResidenceFile,
            TempResidenceDeclaredAt = profile?.TempResidenceDeclaredAt,
            TempResidenceCompleted = profile?.TempResidenceCompleted ?? false
        };
    }

    public async Task<TenantLegalDetailDto> UpdateTenantProfileAsync(
        int tenantId, int ownerUserId, UpdateTenantLegalProfileDto dto, CancellationToken ct = default)
    {
        var tenant = await _legal.GetOwnerTenantByIdAsync(tenantId, ownerUserId, ct)
            ?? throw new InvalidOperationException("Không tìm thấy khách thuê.");

        var profile = await _legal.GetOrCreateTenantLegalProfileAsync(tenantId, ct);

        if (dto.EmergencyContactName != null) profile.EmergencyContactName = dto.EmergencyContactName;
        if (dto.EmergencyContactPhone != null) profile.EmergencyContactPhone = dto.EmergencyContactPhone;
        if (dto.EmergencyContactRelation != null) profile.EmergencyContactRelation = dto.EmergencyContactRelation;
        if (dto.TempResidenceCompleted.HasValue) profile.TempResidenceCompleted = dto.TempResidenceCompleted.Value;
        if (dto.TempResidenceDeclaredAt.HasValue) profile.TempResidenceDeclaredAt = dto.TempResidenceDeclaredAt;

        profile.UpdatedAt = DateTime.Now;
        await _legal.SaveChangesAsync(ct);

        return (await GetTenantDetailAsync(tenantId, ownerUserId, ct))!;
    }

    public async Task<LegalUploadResponseDto> UploadTenantDocumentAsync(
        int tenantId, int ownerUserId, string docType, IFormFile file, CancellationToken ct = default)
    {
        _ = await _legal.GetOwnerTenantByIdAsync(tenantId, ownerUserId, ct)
            ?? throw new InvalidOperationException("Không tìm thấy khách thuê.");

        ValidateFile(file);
        var profile = await _legal.GetOrCreateTenantLegalProfileAsync(tenantId, ct);
        var folder = docType.ToLowerInvariant() switch
        {
            "deposit-receipt" => "uploads/legal/deposit-receipts",
            "temp-residence" => "uploads/legal/temp-residence",
            _ => throw new InvalidOperationException("Loại tài liệu không hợp lệ.")
        };

        var url = await _fileStorage.UploadFormFileAsync(file, folder, $"{tenantId}_{docType}");

        if (docType.Equals("deposit-receipt", StringComparison.OrdinalIgnoreCase))
            profile.DepositReceiptFile = url;
        else
        {
            profile.TempResidenceFile = url;
            profile.TempResidenceCompleted = true;
            profile.TempResidenceDeclaredAt ??= DateTime.Now;
        }

        profile.UpdatedAt = DateTime.Now;
        await _legal.SaveChangesAsync(ct);

        return new LegalUploadResponseDto { FileUrl = url };
    }

    public async Task<List<RoomLegalSummaryDto>> GetRoomSummariesAsync(int ownerUserId, CancellationToken ct = default)
    {
        var rooms = await _legal.GetOwnerRoomsWithDetailsAsync(ownerUserId, ct);
        var tenantProfiles = await LoadTenantProfilesAsync(
            rooms.SelectMany(r => r.Contracts.Where(c => c.Status == "Active").Select(c => c.TenantId)).Distinct(), ct);
        var roomProfiles = await LoadRoomProfilesAsync(rooms.Select(r => r.RoomId), ct);

        return rooms
            .Select(r => BuildRoomSummary(r, tenantProfiles, roomProfiles))
            .OrderBy(r => r.CompletionPercent)
            .ThenBy(r => r.BuildingName)
            .ThenBy(r => r.RoomName)
            .ToList();
    }

    public async Task<RoomLegalDetailDto?> GetRoomDetailAsync(int roomId, int ownerUserId, CancellationToken ct = default)
    {
        var room = await _legal.GetOwnerRoomByIdAsync(roomId, ownerUserId, ct);
        if (room == null) return null;

        var tenantProfiles = await LoadTenantProfilesAsync(
            room.Contracts.Where(c => c.Status == "Active").Select(c => c.TenantId), ct);
        var roomProfile = await _legal.GetRoomLegalProfileAsync(roomId, ct);
        var roomProfiles = roomProfile != null
            ? new Dictionary<int, RoomLegalProfile> { [roomId] = roomProfile }
            : new Dictionary<int, RoomLegalProfile>();

        var summary = BuildRoomSummary(room, tenantProfiles, roomProfiles);
        var activeContract = GetActiveContract(room);

        return new RoomLegalDetailDto
        {
            RoomId = summary.RoomId,
            RoomName = summary.RoomName,
            BuildingName = summary.BuildingName,
            BuildingId = summary.BuildingId,
            RoomStatus = summary.RoomStatus,
            TenantName = summary.TenantName,
            TenantId = summary.TenantId,
            CompletionPercent = summary.CompletionPercent,
            CompletedCount = summary.CompletedCount,
            TotalCount = summary.TotalCount,
            Items = summary.Items,
            HandoverRecordFile = roomProfile?.HandoverRecordFile,
            HandoverCompleted = roomProfile?.HandoverCompleted ?? false,
            AssetConditionNote = roomProfile?.AssetConditionNote,
            ActiveContractId = activeContract?.ContractId,
            ContractStatus = activeContract?.Status,
            DepositStatus = activeContract?.DepositStatus,
            ContractEndDate = activeContract?.EndDate
        };
    }

    public async Task<RoomLegalDetailDto> UpdateRoomProfileAsync(
        int roomId, int ownerUserId, UpdateRoomLegalProfileDto dto, CancellationToken ct = default)
    {
        _ = await _legal.GetOwnerRoomByIdAsync(roomId, ownerUserId, ct)
            ?? throw new InvalidOperationException("Không tìm thấy phòng.");

        var profile = await _legal.GetOrCreateRoomLegalProfileAsync(roomId, ct);
        if (dto.HandoverCompleted.HasValue) profile.HandoverCompleted = dto.HandoverCompleted.Value;
        if (dto.AssetConditionNote != null) profile.AssetConditionNote = dto.AssetConditionNote;
        profile.UpdatedAt = DateTime.Now;
        await _legal.SaveChangesAsync(ct);

        return (await GetRoomDetailAsync(roomId, ownerUserId, ct))!;
    }

    public async Task<LegalUploadResponseDto> UploadRoomHandoverAsync(
        int roomId, int ownerUserId, IFormFile file, CancellationToken ct = default)
    {
        _ = await _legal.GetOwnerRoomByIdAsync(roomId, ownerUserId, ct)
            ?? throw new InvalidOperationException("Không tìm thấy phòng.");

        ValidateFile(file);
        var profile = await _legal.GetOrCreateRoomLegalProfileAsync(roomId, ct);
        var url = await _fileStorage.UploadFormFileAsync(file, "uploads/legal/handover", $"{roomId}_handover");
        profile.HandoverRecordFile = url;
        profile.HandoverCompleted = true;
        profile.UpdatedAt = DateTime.Now;
        await _legal.SaveChangesAsync(ct);

        return new LegalUploadResponseDto { FileUrl = url };
    }

    public async Task<List<BuildingLegalDocumentDto>> GetBuildingDocumentsAsync(
        int? buildingId, int ownerUserId, CancellationToken ct = default)
    {
        var docs = await _legal.GetBuildingDocumentsAsync(buildingId, ownerUserId, ct);
        return docs.Select(MapBuildingDocument).ToList();
    }

    public async Task<BuildingLegalDocumentDto> CreateBuildingDocumentAsync(
        int buildingId, int ownerUserId, CreateBuildingLegalDocumentDto dto, CancellationToken ct = default)
    {
        _ = await _legal.GetOwnerBuildingAsync(buildingId, ownerUserId, ct)
            ?? throw new InvalidOperationException("Không tìm thấy tòa nhà.");

        var document = new BuildingLegalDocument
        {
            BuildingId = buildingId,
            DocumentType = NormalizeDocumentType(dto.DocumentType),
            Title = dto.Title.Trim(),
            IssueDate = dto.IssueDate,
            ExpiryDate = dto.ExpiryDate,
            Note = dto.Note
        };

        _legal.AddBuildingDocument(document);
        await _legal.SaveChangesAsync(ct);

        var saved = await _legal.GetBuildingDocumentByIdAsync(document.BuildingLegalDocumentId, ownerUserId, ct);
        return MapBuildingDocument(saved!);
    }

    public async Task<BuildingLegalDocumentDto?> UpdateBuildingDocumentAsync(
        int documentId, int ownerUserId, UpdateBuildingLegalDocumentDto dto, CancellationToken ct = default)
    {
        var document = await _legal.GetBuildingDocumentByIdAsync(documentId, ownerUserId, ct);
        if (document == null) return null;

        if (dto.DocumentType != null) document.DocumentType = NormalizeDocumentType(dto.DocumentType);
        if (dto.Title != null) document.Title = dto.Title.Trim();
        if (dto.IssueDate.HasValue) document.IssueDate = dto.IssueDate;
        if (dto.ExpiryDate.HasValue) document.ExpiryDate = dto.ExpiryDate;
        if (dto.Note != null) document.Note = dto.Note;
        document.UpdatedAt = DateTime.Now;

        await _legal.SaveChangesAsync(ct);
        return MapBuildingDocument(document);
    }

    public async Task<bool> DeleteBuildingDocumentAsync(int documentId, int ownerUserId, CancellationToken ct = default)
    {
        var document = await _legal.GetBuildingDocumentByIdAsync(documentId, ownerUserId, ct);
        if (document == null) return false;

        if (!string.IsNullOrWhiteSpace(document.FileUrl))
            await _fileStorage.DeleteAsync(document.FileUrl);

        _legal.RemoveBuildingDocument(document);
        await _legal.SaveChangesAsync(ct);
        return true;
    }

    public async Task<LegalUploadResponseDto> UploadBuildingDocumentFileAsync(
        int documentId, int ownerUserId, IFormFile file, CancellationToken ct = default)
    {
        var document = await _legal.GetBuildingDocumentByIdAsync(documentId, ownerUserId, ct)
            ?? throw new InvalidOperationException("Không tìm thấy giấy tờ.");

        ValidateFile(file);
        var url = await _fileStorage.UploadFormFileAsync(
            file, "uploads/legal/building-docs", $"{documentId}_{document.DocumentType}");
        document.FileUrl = url;
        document.UpdatedAt = DateTime.Now;
        await _legal.SaveChangesAsync(ct);

        return new LegalUploadResponseDto { FileUrl = url };
    }

    public async Task<SyncNotificationsResultDto> SyncNotificationsAsync(int ownerUserId, CancellationToken ct = default)
    {
        var alerts = await GetAlertsAsync(ownerUserId, ct);
        var created = 0;

        foreach (var alert in alerts)
        {
            var before = await _notifications.GetUnreadCountAsync(ownerUserId, ct);
            await _notifications.CreateIfNotExistsAsync(ownerUserId, alert.Type, alert.Title, alert.Message, ct);
            var after = await _notifications.GetUnreadCountAsync(ownerUserId, ct);
            if (after > before) created++;
        }

        return new SyncNotificationsResultDto
        {
            CreatedCount = created,
            Message = created > 0
                ? $"Đã tạo {created} thông báo mới."
                : "Không có thông báo mới cần tạo."
        };
    }

    private async Task<Dictionary<int, TenantLegalProfile>> LoadTenantProfilesAsync(
        IEnumerable<int> tenantIds, CancellationToken ct)
    {
        var ids = tenantIds.Distinct().ToList();
        var result = new Dictionary<int, TenantLegalProfile>();
        foreach (var id in ids)
        {
            var profile = await _legal.GetTenantLegalProfileAsync(id, ct);
            if (profile != null) result[id] = profile;
        }
        return result;
    }

    private async Task<Dictionary<int, RoomLegalProfile>> LoadRoomProfilesAsync(
        IEnumerable<int> roomIds, CancellationToken ct)
    {
        var ids = roomIds.Distinct().ToList();
        var result = new Dictionary<int, RoomLegalProfile>();
        foreach (var id in ids)
        {
            var profile = await _legal.GetRoomLegalProfileAsync(id, ct);
            if (profile != null) result[id] = profile;
        }
        return result;
    }

    private TenantLegalSummaryDto BuildTenantSummary(Tenant tenant, TenantLegalProfile? profile)
    {
        var activeContract = GetActiveContract(tenant);
        var items = BuildTenantItems(tenant, activeContract, profile);
        var completed = items.Count(i => i.IsCompleted);

        return new TenantLegalSummaryDto
        {
            TenantId = tenant.TenantId,
            FullName = tenant.FullName,
            RoomName = activeContract?.Room?.RoomName,
            RoomId = activeContract?.RoomId,
            Items = items,
            CompletedCount = completed,
            TotalCount = items.Count,
            CompletionPercent = items.Count == 0 ? 0 : (int)Math.Round(completed * 100.0 / items.Count),
            TempResidencePending = IsTempResidencePending(tenant, profile)
        };
    }

    private RoomLegalSummaryDto BuildRoomSummary(
        Room room,
        Dictionary<int, TenantLegalProfile> tenantProfiles,
        Dictionary<int, RoomLegalProfile> roomProfiles)
    {
        var activeContract = GetActiveContract(room);
        var tenant = activeContract?.Tenant;
        var tenantProfile = tenant != null ? tenantProfiles.GetValueOrDefault(tenant.TenantId) : null;
        var roomProfile = roomProfiles.GetValueOrDefault(room.RoomId);
        var items = BuildRoomItems(room, activeContract, tenant, tenantProfile, roomProfile);
        var completed = items.Count(i => i.IsCompleted);
        var isOccupied = activeContract != null;

        return new RoomLegalSummaryDto
        {
            RoomId = room.RoomId,
            RoomName = room.RoomName,
            BuildingName = room.Building?.BuildingName,
            BuildingId = room.BuildingId,
            RoomStatus = room.Status,
            TenantName = tenant?.FullName,
            TenantId = tenant?.TenantId,
            Items = items,
            CompletedCount = completed,
            TotalCount = isOccupied ? items.Count : 0,
            CompletionPercent = !isOccupied || items.Count == 0
                ? (isOccupied ? 0 : 100)
                : (int)Math.Round(completed * 100.0 / items.Count)
        };
    }

    private static List<LegalChecklistItemDto> BuildTenantItems(
        Tenant tenant, Contract? activeContract, TenantLegalProfile? profile)
    {
        var depositOk = !string.IsNullOrWhiteSpace(profile?.DepositReceiptFile)
            || (activeContract != null && activeContract.Deposit > 0 &&
                !string.Equals(activeContract.DepositStatus, "Pending", StringComparison.OrdinalIgnoreCase));

        return
        [
            Item("cccd_number", TenantItemLabels["cccd_number"], !string.IsNullOrWhiteSpace(tenant.CCCD)),
            Item("cccd_image", TenantItemLabels["cccd_image"], !string.IsNullOrWhiteSpace(tenant.CCCDImage), tenant.CCCDImage),
            Item("rental_contract", TenantItemLabels["rental_contract"],
                activeContract != null && !string.IsNullOrWhiteSpace(activeContract.ContractFile),
                activeContract?.ContractFile, activeContract?.Status),
            Item("emergency_contact", TenantItemLabels["emergency_contact"],
                !string.IsNullOrWhiteSpace(profile?.EmergencyContactName) &&
                !string.IsNullOrWhiteSpace(profile?.EmergencyContactPhone)),
            Item("deposit_receipt", TenantItemLabels["deposit_receipt"], depositOk, profile?.DepositReceiptFile,
                activeContract?.DepositStatus),
            Item("temp_residence", TenantItemLabels["temp_residence"],
                profile?.TempResidenceCompleted == true || !string.IsNullOrWhiteSpace(profile?.TempResidenceFile),
                profile?.TempResidenceFile)
        ];
    }

    private static List<LegalChecklistItemDto> BuildRoomItems(
        Room room,
        Contract? activeContract,
        Tenant? tenant,
        TenantLegalProfile? tenantProfile,
        RoomLegalProfile? roomProfile)
    {
        if (activeContract == null)
        {
            return RoomItemLabels.Keys
                .Select(k => Item(k, RoomItemLabels[k], true, note: "Phòng trống"))
                .ToList();
        }

        var depositOk = activeContract.Deposit <= 0 ||
            !string.Equals(activeContract.DepositStatus, "Pending", StringComparison.OrdinalIgnoreCase);
        var tempOk = tenantProfile?.TempResidenceCompleted == true ||
            !string.IsNullOrWhiteSpace(tenantProfile?.TempResidenceFile);

        return
        [
            Item("tenant_info", RoomItemLabels["tenant_info"],
                tenant != null && !string.IsNullOrWhiteSpace(tenant.FullName) &&
                !string.IsNullOrWhiteSpace(tenant.PhoneNumber)),
            Item("valid_contract", RoomItemLabels["valid_contract"],
                string.Equals(activeContract.Status, "Active", StringComparison.OrdinalIgnoreCase) &&
                activeContract.EndDate.Date >= DateTime.UtcNow.Date,
                activeContract.ContractFile, activeContract.Status),
            Item("deposit_status", RoomItemLabels["deposit_status"], depositOk, status: activeContract.DepositStatus),
            Item("temp_residence", RoomItemLabels["temp_residence"], tempOk, tenantProfile?.TempResidenceFile),
            Item("handover_record", RoomItemLabels["handover_record"],
                roomProfile?.HandoverCompleted == true || !string.IsNullOrWhiteSpace(roomProfile?.HandoverRecordFile),
                roomProfile?.HandoverRecordFile),
            Item("asset_photos", RoomItemLabels["asset_photos"], room.RoomImages.Count > 0)
        ];
    }

    private async Task<List<LegalAlertDto>> BuildAlertsAsync(
        List<Room> rooms,
        List<Tenant> tenants,
        List<BuildingLegalDocument> documents,
        Dictionary<int, TenantLegalProfile> tenantProfiles,
        Dictionary<int, RoomLegalProfile> roomProfiles,
        CancellationToken ct)
    {
        var alerts = new List<LegalAlertDto>();
        var today = DateTime.UtcNow.Date;

        foreach (var room in rooms.Where(HasActiveContract))
        {
            var summary = BuildRoomSummary(room, tenantProfiles, roomProfiles);
            if (summary.CompletionPercent < 100)
            {
                var missing = summary.Items.Where(i => !i.IsCompleted).Select(i => i.Label).ToList();
                alerts.Add(new LegalAlertDto
                {
                    Id = $"room-incomplete-{room.RoomId}",
                    Type = "room_incomplete",
                    Severity = "warning",
                    Title = $"Phòng {room.RoomName} thiếu hồ sơ",
                    Message = $"Còn thiếu: {string.Join(", ", missing)}",
                    RoomId = room.RoomId,
                    TenantId = summary.TenantId
                });
            }
        }

        foreach (var tenant in tenants)
        {
            var profile = tenantProfiles.GetValueOrDefault(tenant.TenantId);
            if (IsTempResidencePending(tenant, profile))
            {
                var activeContract = GetActiveContract(tenant);
                alerts.Add(new LegalAlertDto
                {
                    Id = $"temp-residence-{tenant.TenantId}",
                    Type = "temp_residence_pending",
                    Severity = "danger",
                    Title = $"Chưa khai báo tạm trú — {tenant.FullName}",
                    Message = $"Khách đã nhận phòng từ {tenant.MoveInDate:dd/MM/yyyy}, cần hoàn tất khai báo tạm trú.",
                    TenantId = tenant.TenantId,
                    RoomId = activeContract?.RoomId
                });
            }

            var tenantSummary = BuildTenantSummary(tenant, profile);
            var incomplete = tenantSummary.Items.Where(i => !i.IsCompleted).ToList();
            if (incomplete.Count > 0)
            {
                alerts.Add(new LegalAlertDto
                {
                    Id = $"tenant-incomplete-{tenant.TenantId}",
                    Type = "tenant_incomplete",
                    Severity = "info",
                    Title = $"Hồ sơ khách {tenant.FullName} chưa đầy đủ",
                    Message = $"Thiếu {incomplete.Count} hạng mục: {string.Join(", ", incomplete.Select(i => i.Label))}",
                    TenantId = tenant.TenantId,
                    RoomId = tenantSummary.RoomId
                });
            }
        }

        foreach (var room in rooms)
        {
            var contract = GetActiveContract(room);
            if (contract == null) continue;

            var daysLeft = (contract.EndDate.Date - today).Days;
            if (daysLeft is >= 0 and <= ContractExpiryWarningDays)
            {
                alerts.Add(new LegalAlertDto
                {
                    Id = $"contract-{contract.ContractId}",
                    Type = "contract_expiring",
                    Severity = daysLeft <= 7 ? "danger" : "warning",
                    Title = $"Hợp đồng sắp hết hạn — {contract.Tenant?.FullName}",
                    Message = $"Phòng {room.RoomName} hết hạn ngày {contract.EndDate:dd/MM/yyyy} ({daysLeft} ngày).",
                    ContractId = contract.ContractId,
                    RoomId = room.RoomId,
                    TenantId = contract.TenantId,
                    DueDate = contract.EndDate
                });
            }
        }

        foreach (var doc in documents)
        {
            if (!doc.ExpiryDate.HasValue) continue;
            var daysLeft = (doc.ExpiryDate.Value.Date - today).Days;
            if (daysLeft < 0)
            {
                alerts.Add(new LegalAlertDto
                {
                    Id = $"doc-expired-{doc.BuildingLegalDocumentId}",
                    Type = "document_expired",
                    Severity = "danger",
                    Title = $"Giấy tờ đã hết hạn — {doc.Title}",
                    Message = $"{doc.Building.BuildingName}: hết hạn ngày {doc.ExpiryDate:dd/MM/yyyy}.",
                    BuildingId = doc.BuildingId,
                    DocumentId = doc.BuildingLegalDocumentId,
                    DueDate = doc.ExpiryDate
                });
            }
            else if (daysLeft <= DocumentExpiryWarningDays)
            {
                alerts.Add(new LegalAlertDto
                {
                    Id = $"doc-expiring-{doc.BuildingLegalDocumentId}",
                    Type = "document_expiring",
                    Severity = "warning",
                    Title = $"Giấy tờ sắp hết hạn — {doc.Title}",
                    Message = $"{doc.Building.BuildingName}: còn {daysLeft} ngày (hết hạn {doc.ExpiryDate:dd/MM/yyyy}).",
                    BuildingId = doc.BuildingId,
                    DocumentId = doc.BuildingLegalDocumentId,
                    DueDate = doc.ExpiryDate
                });
            }
        }

        return alerts
            .OrderByDescending(a => a.Severity == "danger")
            .ThenByDescending(a => a.Severity == "warning")
            .ThenBy(a => a.DueDate ?? DateTime.MaxValue)
            .ToList();
    }

    private int CalculateLegalScore(
        List<Room> rooms,
        List<Tenant> tenants,
        List<BuildingLegalDocument> documents,
        Dictionary<int, TenantLegalProfile> tenantProfiles,
        Dictionary<int, RoomLegalProfile> roomProfiles)
    {
        var scores = new List<int>();

        foreach (var room in rooms.Where(HasActiveContract))
        {
            var summary = BuildRoomSummary(room, tenantProfiles, roomProfiles);
            scores.Add(summary.CompletionPercent);
        }

        foreach (var tenant in tenants)
        {
            var summary = BuildTenantSummary(tenant, tenantProfiles.GetValueOrDefault(tenant.TenantId));
            scores.Add(summary.CompletionPercent);
        }

        foreach (var doc in documents)
        {
            var complete = !string.IsNullOrWhiteSpace(doc.FileUrl);
            if (doc.ExpiryDate.HasValue && doc.ExpiryDate.Value.Date < DateTime.UtcNow.Date)
                complete = false;
            scores.Add(complete ? 100 : 0);
        }

        return scores.Count == 0 ? 100 : (int)Math.Round(scores.Average());
    }

    private static bool IsTempResidencePending(Tenant tenant, TenantLegalProfile? profile)
    {
        if (profile?.TempResidenceCompleted == true || !string.IsNullOrWhiteSpace(profile?.TempResidenceFile))
            return false;

        if (!tenant.MoveInDate.HasValue) return false;

        var daysSinceMoveIn = (DateTime.UtcNow.Date - tenant.MoveInDate.Value.Date).Days;
        return daysSinceMoveIn > TempResidenceGraceDays;
    }

    private static Contract? GetActiveContract(Tenant tenant) =>
        tenant.Contracts
            .Where(c => string.Equals(c.Status, "Active", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(c => c.StartDate)
            .FirstOrDefault();

    private static Contract? GetActiveContract(Room room) =>
        room.Contracts
            .Where(c => string.Equals(c.Status, "Active", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(c => c.StartDate)
            .FirstOrDefault();

    private static bool HasActiveContract(Room room) => GetActiveContract(room) != null;

    private static LegalChecklistItemDto Item(
        string key, string label, bool completed, string? fileUrl = null, string? status = null, string? note = null) =>
        new()
        {
            Key = key,
            Label = label,
            IsCompleted = completed,
            FileUrl = fileUrl,
            Status = status,
            Note = note
        };

    private static BuildingLegalDocumentDto MapBuildingDocument(BuildingLegalDocument doc)
    {
        var today = DateTime.UtcNow.Date;
        int? daysUntil = doc.ExpiryDate.HasValue ? (doc.ExpiryDate.Value.Date - today).Days : null;

        return new BuildingLegalDocumentDto
        {
            Id = doc.BuildingLegalDocumentId,
            BuildingId = doc.BuildingId,
            BuildingName = doc.Building?.BuildingName ?? "",
            DocumentType = doc.DocumentType,
            Title = doc.Title,
            FileUrl = doc.FileUrl,
            IssueDate = doc.IssueDate,
            ExpiryDate = doc.ExpiryDate,
            Note = doc.Note,
            IsExpired = daysUntil < 0,
            IsExpiringSoon = daysUntil is >= 0 and <= DocumentExpiryWarningDays,
            DaysUntilExpiry = daysUntil
        };
    }

    private static string NormalizeDocumentType(string? type) => type?.Trim().ToUpperInvariant() switch
    {
        "PCCC" => "PCCC",
        "BUSINESSLICENSE" or "BUSINESS_LICENSE" => "BusinessLicense",
        "UTILITY" => "Utility",
        _ => "Other"
    };

    private static void ValidateFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new InvalidOperationException("Vui lòng chọn file để tải lên.");

        var allowed = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".webp" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext))
            throw new InvalidOperationException("Chỉ chấp nhận file PDF, JPG, PNG hoặc WEBP.");
    }
}
