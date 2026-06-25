using Backend.Authorization;
using Backend.Data;
using Backend.DTOs.Admin;
using Backend.Entities;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class OwnerFeatureService : IOwnerFeatureService
{
    private readonly RentalManagementDb _context;
    private readonly IAdminRepository _adminRepository;
    private readonly IAuditLogService _auditLog;

    public OwnerFeatureService(
        RentalManagementDb context,
        IAdminRepository adminRepository,
        IAuditLogService auditLog)
    {
        _context = context;
        _adminRepository = adminRepository;
        _auditLog = auditLog;
    }

    public async Task<bool> HasFeatureAsync(int ownerUserId, PackageFeature feature, CancellationToken ct = default)
    {
        var features = await GetEffectiveFeaturesAsync(ownerUserId, ct);
        return features.Contains(feature);
    }

    public async Task<IReadOnlySet<PackageFeature>> GetEffectiveFeaturesAsync(int ownerUserId, CancellationToken ct = default)
    {
        var packageName = await GetActivePackageNameAsync(ownerUserId, ct);
        var fromPackage = PackageCatalog.GetPackageFeatures(packageName);
        var manual = await GetActiveManualGrantsAsync(ownerUserId, ct);

        var effective = new HashSet<PackageFeature>(fromPackage);
        foreach (var feature in manual)
            effective.Add(feature);

        return effective;
    }

    public async Task<OwnerFeatureGrantsDto> GetOwnerFeatureGrantsAsync(int ownerId, CancellationToken ct = default)
    {
        var owner = await _adminRepository.GetOwnerByIdAsync(ownerId)
            ?? throw new KeyNotFoundException();

        var packageName = owner.Package;
        var fromPackage = PackageCatalog.GetPackageFeatures(packageName);
        List<OwnerFeatureGrant> grants;
        try
        {
            grants = await _context.OwnerFeatureGrants
                .AsNoTracking()
                .Where(g => g.OwnerUserId == ownerId)
                .ToListAsync(ct);
        }
        catch (Exception ex) when (IsMissingFeatureGrantsTable(ex))
        {
            throw new InvalidOperationException(
                "Bảng OwnerFeatureGrants chưa tồn tại trên database. Vui lòng deploy lại backend và kiểm tra migration AddOwnerFeatureGrants.",
                ex);
        }

        var now = DateTime.Now;
        var features = Enum.GetValues<PackageFeature>()
            .Select(feature =>
            {
                var grant = grants.FirstOrDefault(g =>
                    string.Equals(g.Feature, feature.ToString(), StringComparison.OrdinalIgnoreCase));
                var includedInPackage = fromPackage.Contains(feature);
                var manuallyGranted = grant != null && (!grant.ExpiresAt.HasValue || grant.ExpiresAt >= now);

                return new OwnerFeatureGrantItemDto
                {
                    Feature = feature.ToString(),
                    Label = PackageCatalog.GetDisplayName(feature),
                    RequiredPackage = PackageCatalog.GetRequiredPackageName(feature),
                    IncludedInPackage = includedInPackage,
                    ManuallyGranted = manuallyGranted,
                    IsEffective = includedInPackage || manuallyGranted,
                    ExpiresAt = grant?.ExpiresAt,
                    Note = grant?.Note
                };
            })
            .ToList();

        return new OwnerFeatureGrantsDto
        {
            OwnerId = ownerId,
            PackageName = packageName,
            Features = features
        };
    }

    public async Task<OwnerFeatureGrantsDto> UpdateOwnerFeatureGrantsAsync(
        int ownerId,
        UpdateOwnerFeatureGrantsDto dto,
        int? adminUserId,
        string? ip,
        CancellationToken ct = default)
    {
        _ = await _adminRepository.GetOwnerByIdAsync(ownerId)
            ?? throw new KeyNotFoundException();

        var existing = await _context.OwnerFeatureGrants
            .Where(g => g.OwnerUserId == ownerId)
            .ToListAsync(ct);

        var now = DateTime.Now;
        var changes = new List<string>();

        foreach (var item in dto.Grants ?? [])
        {
            if (!Enum.TryParse<PackageFeature>(item.Feature, ignoreCase: true, out var feature))
                throw new InvalidOperationException($"Tính năng không hợp lệ: {item.Feature}");

            var featureName = feature.ToString();
            var grant = existing.FirstOrDefault(g =>
                string.Equals(g.Feature, featureName, StringComparison.OrdinalIgnoreCase));

            if (item.Granted)
            {
                if (grant == null)
                {
                    grant = new OwnerFeatureGrant
                    {
                        OwnerUserId = ownerId,
                        Feature = featureName,
                        GrantedByUserId = adminUserId,
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    _context.OwnerFeatureGrants.Add(grant);
                    existing.Add(grant);
                }

                grant.ExpiresAt = item.ExpiresAt;
                grant.Note = string.IsNullOrWhiteSpace(item.Note) ? null : item.Note.Trim();
                grant.GrantedByUserId = adminUserId;
                grant.UpdatedAt = now;
                changes.Add($"+{PackageCatalog.GetDisplayName(feature)}");
            }
            else if (grant != null)
            {
                _context.OwnerFeatureGrants.Remove(grant);
                existing.Remove(grant);
                changes.Add($"-{PackageCatalog.GetDisplayName(feature)}");
            }
        }

        await _context.SaveChangesAsync(ct);

        if (changes.Count > 0)
        {
            await _auditLog.LogAsync(
                adminUserId,
                "Update",
                "OwnerFeatureGrant",
                ownerId,
                string.Join(", ", changes),
                ip);
        }

        return await GetOwnerFeatureGrantsAsync(ownerId, ct);
    }

    private async Task<string?> GetActivePackageNameAsync(int ownerUserId, CancellationToken ct)
    {
        var now = DateTime.Now;
        return await _context.Subscriptions
            .AsNoTracking()
            .Include(s => s.Package)
            .Where(s =>
                s.OwnerUserId == ownerUserId &&
                s.Status == "Active" &&
                s.EndDate >= now)
            .OrderByDescending(s => s.EndDate)
            .Select(s => s.Package!.PackageName)
            .FirstOrDefaultAsync(ct);
    }

    private async Task<IReadOnlySet<PackageFeature>> GetActiveManualGrantsAsync(int ownerUserId, CancellationToken ct)
    {
        var now = DateTime.Now;
        var grantNames = await _context.OwnerFeatureGrants
            .AsNoTracking()
            .Where(g => g.OwnerUserId == ownerUserId && (g.ExpiresAt == null || g.ExpiresAt >= now))
            .Select(g => g.Feature)
            .ToListAsync(ct);

        var features = new HashSet<PackageFeature>();
        foreach (var name in grantNames)
        {
            if (Enum.TryParse<PackageFeature>(name, ignoreCase: true, out var feature))
                features.Add(feature);
        }

        return features;
    }

    private static bool IsMissingFeatureGrantsTable(Exception ex) =>
        ex switch
        {
            SqlException sql when sql.Message.Contains("OwnerFeatureGrants", StringComparison.OrdinalIgnoreCase) => true,
            InvalidOperationException { InnerException: SqlException inner }
                when inner.Message.Contains("OwnerFeatureGrants", StringComparison.OrdinalIgnoreCase) => true,
            _ => ex.InnerException is not null && IsMissingFeatureGrantsTable(ex.InnerException),
        };
}
