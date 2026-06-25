using Backend.Authorization;
using Backend.Data;
using Backend.DTOs.Package;
using Backend.Entities;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly RentalManagementDb _context;
    private readonly IOwnerFeatureService _ownerFeatureService;

    public SubscriptionService(RentalManagementDb context, IOwnerFeatureService ownerFeatureService)
    {
        _context = context;
        _ownerFeatureService = ownerFeatureService;
    }

    public async Task<IReadOnlyList<PublicPackageDto>> GetPublicPackagesAsync()
    {
        var packages = await _context.Packages
            .AsNoTracking()
            .Where(p => p.IsEnabled)
            .OrderBy(p => p.Price)
            .ToListAsync();

        return packages
            .Select(MapPublicPackage)
            .ToList();
    }

    public async Task<OwnerSubscriptionDto?> GetOwnerSubscriptionAsync(int ownerUserId)
    {
        var now = DateTime.Now;
        var active = await _context.Subscriptions
            .AsNoTracking()
            .Include(s => s.Package)
            .Where(s => s.OwnerUserId == ownerUserId && s.Status == "Active" && s.EndDate >= now)
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefaultAsync();

        var pendingUpgrade = await _context.Subscriptions
            .AsNoTracking()
            .Include(s => s.Package)
            .Where(s => s.OwnerUserId == ownerUserId && s.Status == "Pending" && s.ReplacesSubscriptionId != null)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();

        if (active != null)
        {
            var dto = await MapOwnerSubscriptionAsync(active, ownerUserId);
            if (pendingUpgrade != null)
            {
                dto.HasPendingUpgrade = true;
                dto.PendingPackageId = pendingUpgrade.PackageId;
                dto.PendingPackageName = pendingUpgrade.Package?.PackageName;
                dto.PendingPaymentAmount = pendingUpgrade.PaymentAmount;
            }

            return dto;
        }

        var pending = await GetPrimarySubscriptionQuery(ownerUserId)
            .FirstOrDefaultAsync();

        if (pending != null)
            return await MapOwnerSubscriptionAsync(pending, ownerUserId);

        return await BuildTrialOnlySubscriptionAsync(ownerUserId);
    }

    public async Task<OwnerSubscriptionDto> RequestSubscriptionAsync(int ownerUserId, int packageId)
    {
        var package = await _context.Packages
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PackageId == packageId && p.IsEnabled)
            ?? throw new InvalidOperationException("Gói dịch vụ không tồn tại hoặc đã bị vô hiệu hóa.");

        var now = DateTime.Now;
        var active = await _context.Subscriptions
            .Include(s => s.Package)
            .Where(s => s.OwnerUserId == ownerUserId && s.Status == "Active" && s.EndDate >= now)
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefaultAsync();

        if (active != null)
            return await RequestUpgradeAsync(ownerUserId, package, active, now);

        var pending = await _context.Subscriptions
            .Include(s => s.Package)
            .FirstOrDefaultAsync(s => s.OwnerUserId == ownerUserId && s.Status == "Pending");
        if (pending != null)
        {
            pending.PackageId = packageId;
            pending.PaymentAmount = null;
            pending.ReplacesSubscriptionId = null;
            pending.UpdatedAt = now;
            EnsurePaymentReference(pending);
            await _context.SaveChangesAsync();
            await _context.Entry(pending).Reference(s => s.Package).LoadAsync();
            return await MapOwnerSubscriptionAsync(pending);
        }

        var subscription = new Subscription
        {
            OwnerUserId = ownerUserId,
            PackageId = packageId,
            StartDate = now,
            EndDate = now.AddMonths(1),
            Status = "Pending",
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        subscription.PaymentReference = SubscriptionPaymentHelper.BuildPaymentReference(subscription.SubscriptionId);
        subscription.UpdatedAt = now;
        await _context.SaveChangesAsync();

        await _context.Entry(subscription).Reference(s => s.Package).LoadAsync();
        return await MapOwnerSubscriptionAsync(subscription);
    }

    public async Task<SubscriptionPaymentCheckoutDto?> GetPaymentCheckoutAsync(int ownerUserId)
    {
        var subscription = await _context.Subscriptions
            .AsNoTracking()
            .Include(s => s.Package)
            .Where(s => s.OwnerUserId == ownerUserId && s.Status == "Pending")
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();

        if (subscription?.Package == null)
            return null;

        var amountDue = GetAmountDue(subscription);
        var paymentReference = subscription.PaymentReference
            ?? SubscriptionPaymentHelper.BuildPaymentReference(subscription.SubscriptionId);
        var settings = await GetPlatformPaymentSettingsAsync();
        string? currentPackageName = null;

        if (subscription.ReplacesSubscriptionId.HasValue)
        {
            currentPackageName = await _context.Subscriptions
                .AsNoTracking()
                .Include(s => s.Package)
                .Where(s => s.SubscriptionId == subscription.ReplacesSubscriptionId.Value)
                .Select(s => s.Package!.PackageName)
                .FirstOrDefaultAsync();
        }

        return new SubscriptionPaymentCheckoutDto
        {
            SubscriptionId = subscription.SubscriptionId,
            PackageId = subscription.PackageId,
            PackageName = subscription.Package.PackageName,
            Amount = amountDue,
            PaymentReference = paymentReference,
            TransferContent = SubscriptionPaymentHelper.BuildTransferContent(
                paymentReference,
                subscription.Package.PackageName),
            BankName = settings?.BankName ?? string.Empty,
            BankId = settings?.BankId ?? string.Empty,
            AccountNumber = settings?.AccountNumber ?? string.Empty,
            AccountName = settings?.AccountName ?? string.Empty,
            IsPaymentConfigured = settings?.IsConfigured == true,
            IsUpgrade = subscription.ReplacesSubscriptionId.HasValue,
            CurrentPackageName = currentPackageName,
            FullPackagePrice = subscription.Package.Price
        };
    }

    public async Task<bool> ConfirmPaymentFromWebhookAsync(
        string transferContent,
        decimal amount,
        string? externalTransactionId)
    {
        if (!string.IsNullOrWhiteSpace(externalTransactionId))
        {
            var alreadyProcessed = await _context.SubscriptionPayments
                .AnyAsync(p => p.ExternalTransactionId == externalTransactionId);
            if (alreadyProcessed)
                return true;
        }

        var subscriptionId = SubscriptionPaymentHelper.TryExtractSubscriptionId(transferContent);
        if (subscriptionId == null)
            return false;

        var subscription = await _context.Subscriptions
            .Include(s => s.Package)
            .FirstOrDefaultAsync(s =>
                s.SubscriptionId == subscriptionId.Value &&
                s.Status == "Pending");

        if (subscription?.Package == null)
            return false;

        var expectedAmount = GetAmountDue(subscription);
        if (Math.Abs(amount - expectedAmount) > 1m)
            return false;

        var reference = subscription.PaymentReference
            ?? SubscriptionPaymentHelper.BuildPaymentReference(subscription.SubscriptionId);
        if (!transferContent.Contains(reference, StringComparison.OrdinalIgnoreCase))
            return false;

        return await ActivateSubscriptionWithPaymentAsync(subscription, amount, "VietQR", externalTransactionId);
    }

    public async Task<bool> SimulatePaymentAsync(int ownerUserId)
    {
        var subscription = await _context.Subscriptions
            .Include(s => s.Package)
            .FirstOrDefaultAsync(s => s.OwnerUserId == ownerUserId && s.Status == "Pending");

        if (subscription?.Package == null)
            return false;

        var txId = $"DEV-{subscription.SubscriptionId}-{DateTime.UtcNow.Ticks}";
        return await ActivateSubscriptionWithPaymentAsync(
            subscription,
            GetAmountDue(subscription),
            "DevSimulate",
            txId);
    }

    private async Task<OwnerSubscriptionDto> RequestUpgradeAsync(
        int ownerUserId,
        Package newPackage,
        Subscription activeSubscription,
        DateTime now)
    {
        var currentPackage = activeSubscription.Package
            ?? await _context.Packages.AsNoTracking().FirstOrDefaultAsync(p => p.PackageId == activeSubscription.PackageId)
            ?? throw new InvalidOperationException("Không tìm thấy gói hiện tại.");

        if (newPackage.PackageId == currentPackage.PackageId)
            throw new InvalidOperationException("Bạn đang sử dụng gói này.");

        if (newPackage.Price <= currentPackage.Price)
            throw new InvalidOperationException("Chỉ có thể nâng cấp lên gói cao hơn. Liên hệ admin để hạ cấp gói.");

        await ValidateRoomLimitAsync(ownerUserId, newPackage.MaxRooms);

        var upgradeFee = SubscriptionUpgradeHelper.CalculateUpgradeFee(
            currentPackage.Price,
            newPackage.Price,
            activeSubscription.EndDate,
            now);

        if (upgradeFee <= 0)
            return await ApplyFreeUpgradeAsync(ownerUserId, newPackage, activeSubscription, now);

        var stalePending = await _context.Subscriptions
            .Where(s => s.OwnerUserId == ownerUserId && s.Status == "Pending")
            .ToListAsync();
        foreach (var stale in stalePending)
        {
            stale.Status = "Cancelled";
            stale.UpdatedAt = now;
        }

        var pending = new Subscription
        {
            OwnerUserId = ownerUserId,
            PackageId = newPackage.PackageId,
            StartDate = activeSubscription.StartDate,
            EndDate = activeSubscription.EndDate,
            Status = "Pending",
            PaymentAmount = upgradeFee,
            ReplacesSubscriptionId = activeSubscription.SubscriptionId,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.Subscriptions.Add(pending);
        await _context.SaveChangesAsync();

        pending.PaymentReference = SubscriptionPaymentHelper.BuildPaymentReference(pending.SubscriptionId);
        pending.UpdatedAt = now;
        await _context.SaveChangesAsync();

        await _context.Entry(pending).Reference(s => s.Package).LoadAsync();
        var dto = await MapOwnerSubscriptionAsync(pending);
        dto.IsUpgrade = true;
        return dto;
    }

    private async Task<OwnerSubscriptionDto> ApplyFreeUpgradeAsync(
        int ownerUserId,
        Package newPackage,
        Subscription activeSubscription,
        DateTime now)
    {
        activeSubscription.Status = "Cancelled";
        activeSubscription.UpdatedAt = now;

        var upgraded = new Subscription
        {
            OwnerUserId = ownerUserId,
            PackageId = newPackage.PackageId,
            StartDate = activeSubscription.StartDate,
            EndDate = activeSubscription.EndDate,
            Status = "Active",
            PaymentAmount = 0,
            ReplacesSubscriptionId = activeSubscription.SubscriptionId,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.Subscriptions.Add(upgraded);
        await _context.SaveChangesAsync();

        upgraded.PaymentReference = SubscriptionPaymentHelper.BuildPaymentReference(upgraded.SubscriptionId);
        upgraded.UpdatedAt = now;

        _context.SubscriptionPayments.Add(new SubscriptionPayment
        {
            OwnerUserId = ownerUserId,
            SubscriptionId = upgraded.SubscriptionId,
            Amount = 0,
            PaymentMethod = "Upgrade",
            PaymentDate = now,
            Status = "Success"
        });

        await _context.SaveChangesAsync();
        await _context.Entry(upgraded).Reference(s => s.Package).LoadAsync();

        var dto = await MapOwnerSubscriptionAsync(upgraded);
        dto.IsUpgrade = true;
        dto.PaymentAmount = 0;
        return dto;
    }

    private async Task<bool> ActivateSubscriptionWithPaymentAsync(
        Subscription subscription,
        decimal amount,
        string paymentMethod,
        string? externalTransactionId)
    {
        if (subscription.Status == "Active")
            return true;

        var now = DateTime.Now;

        if (subscription.ReplacesSubscriptionId.HasValue)
        {
            var previous = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.SubscriptionId == subscription.ReplacesSubscriptionId.Value);
            if (previous != null)
            {
                subscription.StartDate = previous.StartDate;
                subscription.EndDate = previous.EndDate;
                previous.Status = "Cancelled";
                previous.UpdatedAt = now;
            }
            else
            {
                subscription.StartDate = now;
                subscription.EndDate = now.AddMonths(1);
            }
        }
        else
        {
            subscription.StartDate = now;
            subscription.EndDate = now.AddMonths(1);
        }

        subscription.Status = "Active";
        subscription.UpdatedAt = now;
        subscription.PaymentReference ??= SubscriptionPaymentHelper.BuildPaymentReference(subscription.SubscriptionId);

        _context.SubscriptionPayments.Add(new SubscriptionPayment
        {
            OwnerUserId = subscription.OwnerUserId,
            SubscriptionId = subscription.SubscriptionId,
            Amount = amount,
            PaymentMethod = paymentMethod,
            PaymentDate = now,
            Status = "Success",
            ExternalTransactionId = externalTransactionId
        });

        await _context.SaveChangesAsync();
        return true;
    }

    private async Task ValidateRoomLimitAsync(int ownerUserId, int maxRooms)
    {
        var roomCount = await _context.Rooms
            .CountAsync(r => r.Building != null && r.Building.UserId == ownerUserId);
        if (roomCount > maxRooms)
            throw new InvalidOperationException($"Bạn đang quản lý {roomCount} phòng, vượt giới hạn gói mới ({maxRooms} phòng).");
    }

    private static decimal GetAmountDue(Subscription subscription) =>
        subscription.PaymentAmount ?? subscription.Package?.Price ?? 0;

    private static void EnsurePaymentReference(Subscription subscription)
    {
        subscription.PaymentReference ??= SubscriptionPaymentHelper.BuildPaymentReference(subscription.SubscriptionId);
    }

    private async Task<PlatformPaymentSetting?> GetPlatformPaymentSettingsAsync()
    {
        return await _context.PlatformPaymentSettings
            .AsNoTracking()
            .FirstOrDefaultAsync();
    }

    private IQueryable<Subscription> GetPrimarySubscriptionQuery(int ownerUserId) =>
        _context.Subscriptions
            .AsNoTracking()
            .Include(s => s.Package)
            .Where(s => s.OwnerUserId == ownerUserId)
            .OrderByDescending(s => s.Status == "Active")
            .ThenByDescending(s => s.Status == "Pending")
            .ThenByDescending(s => s.CreatedAt);

    private static PublicPackageDto MapPublicPackage(Package package)
    {
        var definition = PackageCatalog.Find(package.PackageName);
        var features = PackageFeatureHelper.SplitFeatureLines(package.FeatureLines);
        if (features.Count == 0 && definition != null)
            features = definition.FeatureLines.ToList();

        return new PublicPackageDto
        {
            PackageId = package.PackageId,
            PackageName = package.PackageName,
            RoomRange = package.RoomRange ?? definition?.RoomRange ?? $"Tối đa {package.MaxRooms} phòng",
            TargetAudience = package.TargetAudience ?? definition?.TargetAudience ?? string.Empty,
            Price = package.Price,
            MaxRooms = package.MaxRooms,
            Description = package.Description ?? definition?.Description ?? string.Empty,
            Recommended = package.IsRecommended || (definition?.Recommended ?? false),
            Features = features
        };
    }

    private async Task<OwnerSubscriptionDto> MapOwnerSubscriptionAsync(Subscription subscription, int? ownerUserId = null)
    {
        var package = subscription.Package;
        var features = package == null
            ? []
            : PackageFeatureHelper.SplitFeatureLines(package.FeatureLines);
        if (features.Count == 0)
            features = PackageCatalog.Find(package?.PackageName)?.FeatureLines.ToList() ?? [];

        var ownerId = ownerUserId ?? subscription.OwnerUserId;
        await AppendTrialFeatureLabelsAsync(features, ownerId, package?.PackageName);
        var effective = await _ownerFeatureService.GetEffectiveFeaturesAsync(ownerId);
        var hasTrial = await _ownerFeatureService.HasAnyActiveManualGrantAsync(ownerId);

        return new OwnerSubscriptionDto
        {
            SubscriptionId = subscription.SubscriptionId,
            PackageId = subscription.PackageId,
            PackageName = package?.PackageName,
            Status = subscription.Status,
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            Features = features,
            PaymentReference = subscription.PaymentReference,
            Price = package?.Price,
            PaymentAmount = subscription.PaymentAmount,
            IsUpgrade = subscription.ReplacesSubscriptionId.HasValue,
            HasTrialAccess = hasTrial,
            EffectiveFeatures = effective.Select(f => f.ToString()).ToList()
        };
    }

    private async Task<OwnerSubscriptionDto?> BuildTrialOnlySubscriptionAsync(int ownerUserId)
    {
        var hasTrial = await _ownerFeatureService.HasAnyActiveManualGrantAsync(ownerUserId);
        if (!hasTrial)
            return null;

        var effective = await _ownerFeatureService.GetEffectiveFeaturesAsync(ownerUserId);
        var features = effective
            .Select(PackageCatalog.GetDisplayName)
            .Select(label => $"{label} (Dùng thử)")
            .ToList();

        return new OwnerSubscriptionDto
        {
            Status = "Trial",
            HasTrialAccess = true,
            EffectiveFeatures = effective.Select(f => f.ToString()).ToList(),
            Features = features
        };
    }

    private async Task AppendTrialFeatureLabelsAsync(List<string> features, int ownerUserId, string? packageName)
    {
        var effective = await _ownerFeatureService.GetEffectiveFeaturesAsync(ownerUserId);
        var fromPackage = PackageCatalog.GetPackageFeatures(packageName);

        foreach (var feature in effective)
        {
            if (fromPackage.Contains(feature))
                continue;

            var displayName = PackageCatalog.GetDisplayName(feature);
            if (features.Any(f => f.Contains(displayName, StringComparison.OrdinalIgnoreCase)))
                continue;

            features.Add($"{displayName} (Dùng thử)");
        }
    }
}
