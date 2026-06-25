using Backend.DTOs.Package;

namespace Backend.Services.Interfaces;

public interface ISubscriptionService
{
    Task<IReadOnlyList<PublicPackageDto>> GetPublicPackagesAsync();
    Task<OwnerSubscriptionDto?> GetOwnerSubscriptionAsync(int ownerUserId);
    Task<OwnerSubscriptionDto> RequestSubscriptionAsync(int ownerUserId, int packageId);
}
