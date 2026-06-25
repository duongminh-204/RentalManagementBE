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
        var subscription = await _context.Subscriptions
            .AsNoTracking()
            .Include(s => s.Package)
            .Where(s => s.OwnerUserId == ownerUserId)
            .OrderByDescending(s => s.Status == "Active")
            .ThenByDescending(s => s.Status == "Pending")
            .ThenByDescending(s => s.CreatedAt)
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
            .FirstOrDefaultAsync(s => s.OwnerUserId == ownerUserId && s.Status == "Pending");
        if (pending != null)
        {
            pending.PackageId = packageId;
            pending.UpdatedAt = now;
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
        await _context.Entry(subscription).Reference(s => s.Package).LoadAsync();
        return MapOwnerSubscription(subscription);
    }

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
            Features = features
        };
    }
}
