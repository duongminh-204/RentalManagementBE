using Backend.Authorization;
using Backend.DTOs.Admin;

namespace Backend.Services.Interfaces;

public interface IOwnerFeatureService
{
    Task<bool> HasFeatureAsync(int ownerUserId, PackageFeature feature, CancellationToken ct = default);

    Task<IReadOnlySet<PackageFeature>> GetEffectiveFeaturesAsync(int ownerUserId, CancellationToken ct = default);

    Task<OwnerFeatureGrantsDto> GetOwnerFeatureGrantsAsync(int ownerId, CancellationToken ct = default);

    Task<OwnerFeatureGrantsDto> UpdateOwnerFeatureGrantsAsync(
        int ownerId,
        UpdateOwnerFeatureGrantsDto dto,
        int? adminUserId,
        string? ip,
        CancellationToken ct = default);
}
