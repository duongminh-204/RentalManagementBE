using Backend.Authorization;
using Backend.Data;
using Backend.DTOs.Admin;
using Backend.DTOs.Package;
using Backend.Entities;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace Backend.Services;

public class AdminService : IAdminService
{
    private readonly IAdminRepository _repo;
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogService _auditLog;
    private readonly UserManager<User> _userManager;
    private readonly IUserRoleService _userRoleService;
    private readonly RentalManagementDb _context;

    public AdminService(
        IAdminRepository repo,
        IUserRepository userRepository,
        IAuditLogService auditLog,
        UserManager<User> userManager,
        IUserRoleService userRoleService,
        RentalManagementDb context)
    {
        _repo = repo;
        _userRepository = userRepository;
        _auditLog = auditLog;
        _userManager = userManager;
        _userRoleService = userRoleService;
        _context = context;
    }

    public async Task<AdminDashboardSummaryDto> GetDashboardSummaryAsync()
    {
        await _repo.ExpireSubscriptionsAsync();
        return await _repo.GetDashboardSummaryAsync();
    }

    public async Task<AdminDashboardChartsDto> GetDashboardChartsAsync()
    {
        await _repo.ExpireSubscriptionsAsync();
        return await _repo.GetDashboardChartsAsync();
    }

    public async Task<PagedResultDto<AdminOwnerListDto>> GetOwnersAsync(string? search, string? status, int page, int pageSize)
    {
        await _repo.ExpireSubscriptionsAsync();
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var (items, total) = await _repo.GetOwnersAsync(search, status, page, pageSize);
        return new PagedResultDto<AdminOwnerListDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public Task<AdminOwnerDetailDto?> GetOwnerByIdAsync(int ownerId) => _repo.GetOwnerByIdAsync(ownerId);

    public async Task<AdminOwnerDetailDto> CreateOwnerAsync(CreateAdminOwnerDto dto, int? adminUserId, string? ip)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName) || string.IsNullOrWhiteSpace(dto.Email))
            throw new InvalidOperationException("Họ tên và email là bắt buộc.");

        if (await _userRepository.IsEmailOrPhoneExistAsync(dto.Email, dto.PhoneNumber))
            throw new InvalidOperationException("Email hoặc số điện thoại đã tồn tại.");

        var user = new User
        {
            UserName = dto.Email.Trim(),
            FullName = dto.FullName.Trim(),
            Email = dto.Email.Trim(),
            PhoneNumber = dto.PhoneNumber?.Trim(),
            VisiblePassword = dto.Password,
            IsActive = true,
            IsSuspended = false,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        var createResult = await _userManager.CreateAsync(user, dto.Password);
        if (!createResult.Succeeded)
            throw new InvalidOperationException(string.Join(" ", createResult.Errors.Select(e => e.Description)));

        await _userManager.AddToRoleAsync(user, RoleNames.Owner);

        if (dto.PackageId.HasValue)
            await CreateSubscriptionForOwnerAsync(user.UserId, dto.PackageId.Value, adminUserId, ip);

        await _auditLog.LogAsync(adminUserId, "Create", "Owner", user.UserId, $"Created owner {user.Email}", ip);

        return (await _repo.GetOwnerByIdAsync(user.UserId))!;
    }

    public async Task<AdminOwnerDetailDto> UpdateOwnerAsync(int ownerId, UpdateAdminOwnerDto dto, int? adminUserId, string? ip)
    {
        var owner = await _repo.GetOwnerEntityAsync(ownerId)
            ?? throw new KeyNotFoundException();

        if (await _userRepository.IsEmailOrPhoneTakenByOtherAsync(dto.Email, dto.PhoneNumber, ownerId))
            throw new InvalidOperationException("Email hoặc số điện thoại đã được sử dụng.");

        owner.FullName = dto.FullName.Trim();
        owner.Email = dto.Email?.Trim();
        owner.PhoneNumber = dto.PhoneNumber?.Trim();
        owner.UpdatedAt = DateTime.Now;
        await _repo.SaveChangesAsync();

        if (dto.PackageId.HasValue)
        {
            var active = await _repo.GetActiveSubscriptionAsync(ownerId);
            if (active == null || active.PackageId != dto.PackageId.Value)
                await ChangeOwnerPackageAsync(ownerId, dto.PackageId.Value, adminUserId, ip, isUpgrade: true);
        }

        await _auditLog.LogAsync(adminUserId, "Update", "Owner", ownerId, null, ip);
        return (await _repo.GetOwnerByIdAsync(ownerId))!;
    }

    public async Task DeleteOwnerAsync(int ownerId, int? adminUserId, string? ip)
    {
        var owner = await _repo.GetOwnerEntityAsync(ownerId)
            ?? throw new KeyNotFoundException();

        var roomCount = await _repo.GetOwnerRoomCountAsync(ownerId);
        if (roomCount > 0)
            throw new InvalidOperationException("Không thể xóa chủ trọ đang có phòng.");

        await _repo.CleanupOwnerBeforeDeleteAsync(ownerId);
        await _auditLog.LogAsync(adminUserId, "Delete", "Owner", ownerId, owner.Email, ip);

        var deleteResult = await _userManager.DeleteAsync(owner);
        if (!deleteResult.Succeeded)
            throw new InvalidOperationException(string.Join(" ", deleteResult.Errors.Select(e => e.Description)));
    }

    public async Task<AdminOwnerDetailDto> SuspendOwnerAsync(int ownerId, int? adminUserId, string? ip)
    {
        var owner = await _repo.GetOwnerEntityAsync(ownerId) ?? throw new KeyNotFoundException();
        owner.IsSuspended = true;
        owner.UpdatedAt = DateTime.Now;

        var active = await _repo.GetActiveSubscriptionAsync(ownerId);
        if (active != null)
        {
            active.Status = "Suspended";
            active.UpdatedAt = DateTime.Now;
        }

        await _repo.SaveChangesAsync();
        await _auditLog.LogAsync(adminUserId, "Update", "Owner", ownerId, "Suspended", ip);
        return (await _repo.GetOwnerByIdAsync(ownerId))!;
    }

    public async Task<AdminOwnerDetailDto> ActivateOwnerAsync(int ownerId, int? adminUserId, string? ip)
    {
        var owner = await _repo.GetOwnerEntityAsync(ownerId) ?? throw new KeyNotFoundException();
        owner.IsSuspended = false;
        owner.UpdatedAt = DateTime.Now;

        var suspended = owner.Subscriptions.FirstOrDefault(s => s.Status == "Suspended");
        if (suspended != null && suspended.EndDate >= DateTime.Now)
        {
            suspended.Status = "Active";
            suspended.UpdatedAt = DateTime.Now;
        }

        await _repo.SaveChangesAsync();
        await _auditLog.LogAsync(adminUserId, "Update", "Owner", ownerId, "Activated", ip);
        return (await _repo.GetOwnerByIdAsync(ownerId))!;
    }

    public async Task<AdminOwnerDetailDto> LockOwnerAsync(int ownerId, int? adminUserId, string? ip)
    {
        var owner = await _repo.GetOwnerEntityAsync(ownerId) ?? throw new KeyNotFoundException();
        owner.IsActive = false;
        owner.IsSuspended = false;
        owner.UpdatedAt = DateTime.Now;
        await _repo.SaveChangesAsync();
        await _auditLog.LogAsync(adminUserId, "Update", "Owner", ownerId, "Locked", ip);
        return (await _repo.GetOwnerByIdAsync(ownerId))!;
    }

    public async Task<AdminOwnerDetailDto> UnlockOwnerAsync(int ownerId, int? adminUserId, string? ip)
    {
        var owner = await _repo.GetOwnerEntityAsync(ownerId) ?? throw new KeyNotFoundException();
        owner.IsActive = true;
        owner.IsSuspended = false;
        owner.UpdatedAt = DateTime.Now;
        await _repo.SaveChangesAsync();
        await _auditLog.LogAsync(adminUserId, "Update", "Owner", ownerId, "Unlocked", ip);
        return (await _repo.GetOwnerByIdAsync(ownerId))!;
    }

    public async Task<PagedResultDto<AdminPackageDto>> GetPackagesAsync(string? search, bool? isEnabled, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var (items, total) = await _repo.GetPackagesAsync(search, isEnabled, page, pageSize);
        return new PagedResultDto<AdminPackageDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<AdminPackageDto> CreatePackageAsync(CreatePackageDto dto, int? adminUserId, string? ip)
    {
        if (string.IsNullOrWhiteSpace(dto.PackageName))
            throw new InvalidOperationException("Tên gói là bắt buộc.");

        var package = new Package
        {
            PackageName = dto.PackageName.Trim(),
            Price = dto.Price,
            MaxRooms = dto.MaxRooms,
            Description = dto.Description?.Trim(),
            RoomRange = dto.RoomRange?.Trim(),
            TargetAudience = dto.TargetAudience?.Trim(),
            IsRecommended = dto.IsRecommended,
            FeatureLines = PackageFeatureHelper.JoinFeatureLines(dto.Features),
            IsEnabled = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        await _repo.AddPackageAsync(package);
        await _auditLog.LogAsync(adminUserId, "Create", "Package", package.PackageId, package.PackageName, ip);
        var (items, _) = await _repo.GetPackagesAsync(null, null, 1, 1);
        return items.FirstOrDefault(p => p.PackageId == package.PackageId)
            ?? new AdminPackageDto
            {
                PackageId = package.PackageId,
                PackageName = package.PackageName,
                Price = package.Price,
                MaxRooms = package.MaxRooms,
                Description = package.Description,
                RoomRange = package.RoomRange,
                TargetAudience = package.TargetAudience,
                IsRecommended = package.IsRecommended,
                Features = PackageFeatureHelper.SplitFeatureLines(package.FeatureLines),
                IsEnabled = package.IsEnabled
            };
    }

    public async Task<AdminPackageDto> UpdatePackageAsync(int packageId, UpdatePackageDto dto, int? adminUserId, string? ip)
    {
        var package = await _repo.GetPackageByIdAsync(packageId) ?? throw new KeyNotFoundException();
        package.PackageName = dto.PackageName.Trim();
        package.Price = dto.Price;
        package.MaxRooms = dto.MaxRooms;
        package.Description = dto.Description?.Trim();
        package.RoomRange = dto.RoomRange?.Trim();
        package.TargetAudience = dto.TargetAudience?.Trim();
        package.IsRecommended = dto.IsRecommended;
        package.FeatureLines = PackageFeatureHelper.JoinFeatureLines(dto.Features);
        package.UpdatedAt = DateTime.Now;
        await _repo.SaveChangesAsync();
        await _auditLog.LogAsync(adminUserId, "Update", "Package", packageId, null, ip);
        var (items, _) = await _repo.GetPackagesAsync(null, null, 1, 100);
        return items.First(p => p.PackageId == packageId);
    }

    public async Task<AdminPackageDto> EnablePackageAsync(int packageId, int? adminUserId, string? ip)
    {
        var package = await _repo.GetPackageByIdAsync(packageId) ?? throw new KeyNotFoundException();
        package.IsEnabled = true;
        package.UpdatedAt = DateTime.Now;
        await _repo.SaveChangesAsync();
        await _auditLog.LogAsync(adminUserId, "Update", "Package", packageId, "Enabled", ip);
        var (items, _) = await _repo.GetPackagesAsync(null, null, 1, 100);
        return items.First(p => p.PackageId == packageId);
    }

    public async Task<AdminPackageDto> DisablePackageAsync(int packageId, int? adminUserId, string? ip)
    {
        var package = await _repo.GetPackageByIdAsync(packageId) ?? throw new KeyNotFoundException();
        package.IsEnabled = false;
        package.UpdatedAt = DateTime.Now;
        await _repo.SaveChangesAsync();
        await _auditLog.LogAsync(adminUserId, "Update", "Package", packageId, "Disabled", ip);
        var (items, _) = await _repo.GetPackagesAsync(null, null, 1, 100);
        return items.First(p => p.PackageId == packageId);
    }

    public async Task DeletePackageAsync(int packageId, int? adminUserId, string? ip)
    {
        var package = await _repo.GetPackageByIdAsync(packageId) ?? throw new KeyNotFoundException();
        if (await _repo.PackageHasSubscriptionsAsync(packageId))
            throw new InvalidOperationException("Không thể xóa gói đang có người đăng ký.");

        await _repo.DeletePackageAsync(package);
        await _auditLog.LogAsync(adminUserId, "Delete", "Package", packageId, package.PackageName, ip);
    }

    public async Task<PagedResultDto<AdminSubscriptionDto>> GetSubscriptionsAsync(string? status, string? search, int page, int pageSize)
    {
        await _repo.ExpireSubscriptionsAsync();
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var (items, total) = await _repo.GetSubscriptionsAsync(status, search, page, pageSize);
        return new PagedResultDto<AdminSubscriptionDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<PagedResultDto<AdminOwnerSubscriptionsGroupDto>> GetSubscriptionsGroupedByOwnerAsync(string? status, string? search, int page, int pageSize)
    {
        await _repo.ExpireSubscriptionsAsync();
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var (items, total) = await _repo.GetSubscriptionsGroupedByOwnerAsync(status, search, page, pageSize);
        return new PagedResultDto<AdminOwnerSubscriptionsGroupDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<AdminSubscriptionDto> UpgradeSubscriptionAsync(int subscriptionId, ChangePackageDto dto, int? adminUserId, string? ip)
    {
        var sub = await _repo.GetSubscriptionByIdAsync(subscriptionId) ?? throw new KeyNotFoundException();
        var newPackage = await _repo.GetPackageByIdAsync(dto.PackageId) ?? throw new InvalidOperationException("Gói không tồn tại.");
        if (!newPackage.IsEnabled) throw new InvalidOperationException("Gói đã bị vô hiệu hóa.");
        if (newPackage.Price <= sub.Package.Price)
            throw new InvalidOperationException("Gói mới phải có giá cao hơn gói hiện tại (upgrade).");

        await ValidateRoomLimitAsync(sub.OwnerUserId, newPackage.MaxRooms);
        return await ChangeOwnerPackageAsync(sub.OwnerUserId, dto.PackageId, adminUserId, ip, isUpgrade: true, existingSub: sub);
    }

    public async Task<AdminSubscriptionDto> DowngradeSubscriptionAsync(int subscriptionId, ChangePackageDto dto, int? adminUserId, string? ip)
    {
        var sub = await _repo.GetSubscriptionByIdAsync(subscriptionId) ?? throw new KeyNotFoundException();
        var newPackage = await _repo.GetPackageByIdAsync(dto.PackageId) ?? throw new InvalidOperationException("Gói không tồn tại.");
        if (!newPackage.IsEnabled) throw new InvalidOperationException("Gói đã bị vô hiệu hóa.");
        if (newPackage.Price >= sub.Package.Price)
            throw new InvalidOperationException("Gói mới phải có giá thấp hơn gói hiện tại (downgrade).");

        await ValidateRoomLimitAsync(sub.OwnerUserId, newPackage.MaxRooms);
        return await ChangeOwnerPackageAsync(sub.OwnerUserId, dto.PackageId, adminUserId, ip, isUpgrade: false, existingSub: sub);
    }

    public async Task<AdminSubscriptionDto> RenewSubscriptionAsync(int subscriptionId, int? adminUserId, string? ip)
    {
        var sub = await _repo.GetSubscriptionByIdAsync(subscriptionId) ?? throw new KeyNotFoundException();
        if (sub.Status == "Cancelled")
            throw new InvalidOperationException("Không thể gia hạn gói đã hủy.");

        var now = DateTime.Now;
        sub.EndDate = (sub.EndDate > now ? sub.EndDate : now).AddMonths(1);
        sub.Status = "Active";
        sub.UpdatedAt = now;
        await _repo.SaveChangesAsync();

        await RecordPaymentAsync(sub, sub.Package.Price, "Renewal", adminUserId, ip);
        await _auditLog.LogAsync(adminUserId, "Subscription", "Subscription", subscriptionId, "Renewed", ip);

        var (items, _) = await _repo.GetSubscriptionsAsync(null, null, 1, 100);
        return items.First(s => s.SubscriptionId == subscriptionId);
    }

    public async Task<AdminSubscriptionDto> CancelSubscriptionAsync(int subscriptionId, int? adminUserId, string? ip)
    {
        var sub = await _repo.GetSubscriptionByIdAsync(subscriptionId) ?? throw new KeyNotFoundException();
        sub.Status = "Cancelled";
        sub.UpdatedAt = DateTime.Now;
        await _repo.SaveChangesAsync();
        await _auditLog.LogAsync(adminUserId, "Subscription", "Subscription", subscriptionId, "Cancelled", ip);

        var (items, _) = await _repo.GetSubscriptionsAsync(null, null, 1, 100);
        return items.First(s => s.SubscriptionId == subscriptionId);
    }

    public async Task<AdminSubscriptionDto> ActivateSubscriptionAsync(int subscriptionId, int? adminUserId, string? ip)
    {
        var sub = await _repo.GetSubscriptionByIdAsync(subscriptionId) ?? throw new KeyNotFoundException();
        if (sub.Status != "Pending")
            throw new InvalidOperationException("Chỉ có thể kích hoạt gói đang chờ duyệt (Pending).");

        var now = DateTime.Now;
        sub.StartDate = now;
        sub.EndDate = now.AddMonths(1);
        sub.Status = "Active";
        sub.UpdatedAt = now;
        await _repo.SaveChangesAsync();

        await RecordPaymentAsync(sub, sub.Package.Price, "Activation", adminUserId, ip);
        await _auditLog.LogAsync(adminUserId, "Subscription", "Subscription", subscriptionId, "Activated", ip);

        var (items, _) = await _repo.GetSubscriptionsAsync(null, null, 1, 100);
        return items.First(s => s.SubscriptionId == subscriptionId);
    }

    public async Task DeleteSubscriptionAsync(int subscriptionId, int? adminUserId, string? ip)
    {
        var sub = await _repo.GetSubscriptionByIdAsync(subscriptionId) ?? throw new KeyNotFoundException();
        if (sub.Status == "Active")
            throw new InvalidOperationException("Không thể xóa gói đang hoạt động. Hãy hủy đăng ký trước.");

        await _repo.DeleteSubscriptionAsync(sub);
        await _auditLog.LogAsync(adminUserId, "Delete", "Subscription", subscriptionId,
            $"{sub.Package.PackageName} — {sub.Owner.FullName}", ip);
    }

    public async Task<PagedResultDto<AdminPaymentDto>> GetPaymentsAsync(string? status, int? ownerId, DateTime? from, DateTime? to, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var (items, total) = await _repo.GetPaymentsAsync(status, ownerId, from, to, page, pageSize);
        return new PagedResultDto<AdminPaymentDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public Task<RevenueReportDto> GetRevenueReportAsync(DateTime? from, DateTime? to) =>
        _repo.GetRevenueReportAsync(from, to);

    public async Task<byte[]> ExportPaymentsExcelAsync(DateTime? from, DateTime? to)
    {
        var (items, _) = await _repo.GetPaymentsAsync(null, null, from, to, 1, 10000);
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Payments");
        sheet.Cell(1, 1).Value = "PaymentID";
        sheet.Cell(1, 2).Value = "OwnerID";
        sheet.Cell(1, 3).Value = "OwnerName";
        sheet.Cell(1, 4).Value = "SubscriptionID";
        sheet.Cell(1, 5).Value = "Amount";
        sheet.Cell(1, 6).Value = "PaymentMethod";
        sheet.Cell(1, 7).Value = "PaymentDate";
        sheet.Cell(1, 8).Value = "Status";

        var row = 2;
        foreach (var p in items)
        {
            sheet.Cell(row, 1).Value = p.PaymentId;
            sheet.Cell(row, 2).Value = p.OwnerId;
            sheet.Cell(row, 3).Value = p.OwnerName;
            sheet.Cell(row, 4).Value = p.SubscriptionId;
            sheet.Cell(row, 5).Value = p.Amount;
            sheet.Cell(row, 6).Value = p.PaymentMethod;
            sheet.Cell(row, 7).Value = p.PaymentDate;
            sheet.Cell(row, 8).Value = p.Status;
            row++;
        }

        sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<PagedResultDto<AdminUserDto>> GetUsersAsync(string? role, string? search, bool? isActive, string? subscriptionStatus, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var (items, total) = await _repo.GetUsersAsync(role, search, isActive, subscriptionStatus, page, pageSize);
        return new PagedResultDto<AdminUserDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<AdminUserDto> EnableUserAsync(int userId, int? adminUserId, string? ip)
    {
        var user = await _repo.GetUserByIdAsync(userId) ?? throw new KeyNotFoundException();
        user.IsActive = true;
        user.IsSuspended = false;
        user.UpdatedAt = DateTime.Now;
        await _repo.SaveChangesAsync();
        await _auditLog.LogAsync(adminUserId, "Update", "User", userId, "Enabled", ip);
        return MapUser(user, await _userRoleService.GetPrimaryRoleAsync(user));
    }

    public async Task<AdminUserDto> DisableUserAsync(int userId, int? adminUserId, string? ip)
    {
        var user = await _repo.GetUserByIdAsync(userId) ?? throw new KeyNotFoundException();
        if (await _userRoleService.IsInRoleAsync(user, RoleNames.Admin))
            throw new InvalidOperationException("Không thể vô hiệu hóa tài khoản Admin.");
        user.IsActive = false;
        user.IsSuspended = false;
        user.UpdatedAt = DateTime.Now;
        await _repo.SaveChangesAsync();
        await _auditLog.LogAsync(adminUserId, "Update", "User", userId, "Disabled", ip);
        return MapUser(user, await _userRoleService.GetPrimaryRoleAsync(user));
    }

    public async Task<AdminResetPasswordResultDto> ResetUserPasswordAsync(int userId, int? adminUserId, string? ip)
    {
        var user = await _repo.GetUserByIdAsync(userId) ?? throw new KeyNotFoundException();
        var tempPassword = GenerateTemporaryPassword();
        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, tempPassword);
        if (!resetResult.Succeeded)
            throw new InvalidOperationException(string.Join(" ", resetResult.Errors.Select(e => e.Description)));
        user.UpdatedAt = DateTime.Now;
        await _repo.SaveChangesAsync();
        await _auditLog.LogAsync(adminUserId, "Update", "User", userId, "ResetPassword", ip);
        return new AdminResetPasswordResultDto
        {
            Message = "Đặt lại mật khẩu thành công.",
            TemporaryPassword = tempPassword
        };
    }

    public async Task<AdminUserPasswordDto> GetUserPasswordAsync(int userId)
    {
        var user = await _repo.GetUserByIdAsync(userId) ?? throw new KeyNotFoundException();
        return new AdminUserPasswordDto { Password = user.VisiblePassword };
    }

    public async Task ChangeUserPasswordAsync(int userId, AdminChangePasswordDto dto, int? adminUserId, string? ip)
    {
        if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
            throw new InvalidOperationException("Mật khẩu mới phải có ít nhất 6 ký tự.");

        var user = await _repo.GetUserByIdAsync(userId) ?? throw new KeyNotFoundException();

        if (await _userRoleService.IsInRoleAsync(user, RoleNames.Admin))
            throw new InvalidOperationException("Không thể đổi mật khẩu tài khoản Admin.");

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, dto.NewPassword);
        if (!resetResult.Succeeded)
            throw new InvalidOperationException(string.Join(" ", resetResult.Errors.Select(e => e.Description)));

        user.VisiblePassword = dto.NewPassword;
        user.UpdatedAt = DateTime.Now;
        await _repo.SaveChangesAsync();
        await _auditLog.LogAsync(adminUserId, "Update", "User", userId, "ChangePassword", ip);
    }

    public async Task DeleteUserAsync(int userId, int? adminUserId, string? ip)
    {
        if (adminUserId.HasValue && adminUserId.Value == userId)
            throw new InvalidOperationException("Không thể xóa tài khoản của chính bạn.");

        var user = await _repo.GetUserByIdAsync(userId) ?? throw new KeyNotFoundException();

        if (await _userRoleService.IsInRoleAsync(user, RoleNames.Admin))
            throw new InvalidOperationException("Không thể xóa tài khoản Admin.");

        if (await _userRoleService.IsInRoleAsync(user, RoleNames.Owner))
        {
            await DeleteOwnerAsync(userId, adminUserId, ip);
            return;
        }

        await _auditLog.LogAsync(adminUserId, "Delete", "User", userId, user.Email, ip);
        var deleteResult = await _userManager.DeleteAsync(user);
        if (!deleteResult.Succeeded)
            throw new InvalidOperationException(string.Join(" ", deleteResult.Errors.Select(e => e.Description)));
    }

    public async Task<PagedResultDto<AdminAuditLogDto>> GetAuditLogsAsync(int? userId, string? action, string? entity, DateTime? from, DateTime? to, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var (items, total) = await _repo.GetAuditLogsAsync(userId, action, entity, from, to, page, pageSize);
        return new PagedResultDto<AdminAuditLogDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<int> ClearAuditLogsAsync(int? userId, string? action, string? entity, DateTime? from, DateTime? to, int? adminUserId, string? ip)
    {
        var deletedCount = await _repo.ClearAuditLogsAsync(userId, action, entity, from, to);

        var filterParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(action)) filterParts.Add($"action={action}");
        if (!string.IsNullOrWhiteSpace(entity)) filterParts.Add($"entity={entity}");
        if (from.HasValue) filterParts.Add($"from={from:yyyy-MM-dd}");
        if (to.HasValue) filterParts.Add($"to={to:yyyy-MM-dd}");
        if (userId.HasValue) filterParts.Add($"userId={userId.Value}");

        var details = deletedCount > 0
            ? $"Cleared {deletedCount} audit log(s)"
            : "No audit logs matched filters";
        if (filterParts.Count > 0)
            details += $" ({string.Join(", ", filterParts)})";

        await _auditLog.LogAsync(adminUserId, "Delete", "AuditLog", null, details, ip);
        return deletedCount;
    }

    private async Task<AdminSubscriptionDto> ChangeOwnerPackageAsync(int ownerUserId, int packageId, int? adminUserId, string? ip, bool isUpgrade, Subscription? existingSub = null)
    {
        var package = await _repo.GetPackageByIdAsync(packageId)
            ?? throw new InvalidOperationException("Gói không tồn tại.");

        if (!package.IsEnabled)
            throw new InvalidOperationException("Gói đã bị vô hiệu hóa.");

        await ValidateRoomLimitAsync(ownerUserId, package.MaxRooms);

        if (existingSub != null)
        {
            existingSub.Status = "Cancelled";
            existingSub.UpdatedAt = DateTime.Now;
        }
        else
        {
            var active = await _repo.GetActiveSubscriptionAsync(ownerUserId);
            if (active != null)
            {
                active.Status = "Cancelled";
                active.UpdatedAt = DateTime.Now;
            }
        }

        var now = DateTime.Now;
        var subscription = new Subscription
        {
            OwnerUserId = ownerUserId,
            PackageId = packageId,
            StartDate = now,
            EndDate = now.AddMonths(1),
            Status = "Active",
            CreatedAt = now,
            UpdatedAt = now
        };

        await _repo.AddSubscriptionAsync(subscription);
        await RecordPaymentAsync(subscription, package.Price, isUpgrade ? "Upgrade" : "Downgrade", adminUserId, ip);
        await _auditLog.LogAsync(adminUserId, "Subscription", "Subscription", subscription.SubscriptionId,
            isUpgrade ? "Upgraded" : "Downgraded", ip);

        var (items, _) = await _repo.GetSubscriptionsAsync(null, null, 1, 100);
        return items.First(s => s.SubscriptionId == subscription.SubscriptionId);
    }

    private async Task CreateSubscriptionForOwnerAsync(int ownerUserId, int packageId, int? adminUserId, string? ip)
    {
        await ChangeOwnerPackageAsync(ownerUserId, packageId, adminUserId, ip, isUpgrade: true);
    }

    private async Task ValidateRoomLimitAsync(int ownerUserId, int maxRooms)
    {
        var roomCount = await _repo.GetOwnerRoomCountAsync(ownerUserId);
        if (roomCount > maxRooms)
            throw new InvalidOperationException($"Chủ trọ đang có {roomCount} phòng, vượt giới hạn gói ({maxRooms} phòng).");
    }

    private async Task RecordPaymentAsync(Subscription sub, decimal amount, string method, int? adminUserId, string? ip)
    {
        var payment = new SubscriptionPayment
        {
            OwnerUserId = sub.OwnerUserId,
            SubscriptionId = sub.SubscriptionId,
            Amount = amount,
            PaymentMethod = method,
            PaymentDate = DateTime.Now,
            Status = "Success"
        };
        await _repo.AddPaymentAsync(payment);
        await _auditLog.LogAsync(adminUserId, "Payment", "SubscriptionPayment", payment.PaymentId, method, ip);
    }

    private static AdminUserDto MapUser(User user, string? role = null) => new()
    {
        UserId = user.UserId,
        FullName = user.FullName,
        Email = user.Email,
        PhoneNumber = user.PhoneNumber,
        Address = user.Address,
        Avatar = user.Avatar,
        Role = role ?? string.Empty,
        IsActive = user.IsActive,
        IsSuspended = user.IsSuspended,
        CreatedAt = user.CreatedAt
    };

    private static string GenerateTemporaryPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
        return new string(Enumerable.Range(0, 10).Select(_ => chars[RandomNumberGenerator.GetInt32(chars.Length)]).ToArray());
    }

    public async Task<PlatformPaymentSettingDto> GetPlatformPaymentSettingsAsync()
    {
        var settings = await _context.PlatformPaymentSettings
            .AsNoTracking()
            .FirstOrDefaultAsync();

        return settings == null
            ? new PlatformPaymentSettingDto()
            : MapPlatformPaymentSetting(settings);
    }

    public async Task<PlatformPaymentSettingDto> UpdatePlatformPaymentSettingsAsync(
        UpdatePlatformPaymentSettingDto dto,
        int? adminUserId,
        string? ip)
    {
        if (string.IsNullOrWhiteSpace(dto.BankName) ||
            string.IsNullOrWhiteSpace(dto.BankId) ||
            string.IsNullOrWhiteSpace(dto.AccountNumber) ||
            string.IsNullOrWhiteSpace(dto.AccountName))
        {
            throw new InvalidOperationException("Vui lòng nhập đầy đủ thông tin tài khoản ngân hàng.");
        }

        var settings = await _context.PlatformPaymentSettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new PlatformPaymentSetting();
            _context.PlatformPaymentSettings.Add(settings);
        }

        settings.BankName = dto.BankName.Trim();
        settings.BankId = dto.BankId.Trim();
        settings.AccountNumber = dto.AccountNumber.Trim();
        settings.AccountName = dto.AccountName.Trim();
        settings.IsConfigured = true;
        settings.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();
        await _auditLog.LogAsync(adminUserId, "Update", "PlatformPaymentSetting", settings.Id, settings.BankName, ip);

        return MapPlatformPaymentSetting(settings);
    }

    private static PlatformPaymentSettingDto MapPlatformPaymentSetting(PlatformPaymentSetting settings) => new()
    {
        BankName = settings.BankName,
        BankId = settings.BankId,
        AccountNumber = settings.AccountNumber,
        AccountName = settings.AccountName,
        IsConfigured = settings.IsConfigured,
        UpdatedAt = settings.UpdatedAt
    };
}
