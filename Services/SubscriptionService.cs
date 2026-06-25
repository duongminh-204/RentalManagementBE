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

    public SubscriptionService(RentalManagementDb context)
    {
        _context = context;
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
        var subscription = await GetPrimarySubscriptionQuery(ownerUserId)
            .FirstOrDefaultAsync();

        return subscription == null ? null : MapOwnerSubscription(subscription);
    }

    public async Task<OwnerSubscriptionDto> RequestSubscriptionAsync(int ownerUserId, int packageId)
    {
        var package = await _context.Packages
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PackageId == packageId && p.IsEnabled)
            ?? throw new InvalidOperationException("Gói dịch vụ không tồn tại hoặc đã bị vô hiệu hóa.");

        var now = DateTime.Now;
        var hasActive = await _context.Subscriptions.AnyAsync(s =>
            s.OwnerUserId == ownerUserId &&
            s.Status == "Active" &&
            s.EndDate >= now);
        if (hasActive)
            throw new InvalidOperationException("Bạn đã có gói đang hoạt động.");

        var pending = await _context.Subscriptions
            .Include(s => s.Package)
            .FirstOrDefaultAsync(s => s.OwnerUserId == ownerUserId && s.Status == "Pending");
        if (pending != null)
        {
            pending.PackageId = packageId;
            pending.UpdatedAt = now;
            EnsurePaymentReference(pending);
            await _context.SaveChangesAsync();
            await _context.Entry(pending).Reference(s => s.Package).LoadAsync();
            return MapOwnerSubscription(pending);
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
        return MapOwnerSubscription(subscription);
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

        var paymentReference = subscription.PaymentReference
            ?? SubscriptionPaymentHelper.BuildPaymentReference(subscription.SubscriptionId);
        var settings = await GetPlatformPaymentSettingsAsync();

        return new SubscriptionPaymentCheckoutDto
        {
            SubscriptionId = subscription.SubscriptionId,
            PackageId = subscription.PackageId,
            PackageName = subscription.Package.PackageName,
            Amount = subscription.Package.Price,
            PaymentReference = paymentReference,
            TransferContent = SubscriptionPaymentHelper.BuildTransferContent(
                paymentReference,
                subscription.Package.PackageName),
            BankName = settings?.BankName ?? string.Empty,
            BankId = settings?.BankId ?? string.Empty,
            AccountNumber = settings?.AccountNumber ?? string.Empty,
            AccountName = settings?.AccountName ?? string.Empty,
            IsPaymentConfigured = settings?.IsConfigured == true
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

        var expectedAmount = subscription.Package.Price;
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
            subscription.Package.Price,
            "DevSimulate",
            txId);
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
        subscription.StartDate = now;
        subscription.EndDate = now.AddMonths(1);
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

    private static OwnerSubscriptionDto MapOwnerSubscription(Subscription subscription)
    {
        var package = subscription.Package;
        var features = package == null
            ? []
            : PackageFeatureHelper.SplitFeatureLines(package.FeatureLines);
        if (features.Count == 0)
            features = PackageCatalog.Find(package?.PackageName)?.FeatureLines.ToList() ?? [];

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
            Price = package?.Price
        };
    }
}
