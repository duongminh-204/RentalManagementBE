using Backend.DTOs.Package;

namespace Backend.Services.Interfaces;

public interface ISubscriptionService
{
    Task<IReadOnlyList<PublicPackageDto>> GetPublicPackagesAsync();
    Task<OwnerSubscriptionDto?> GetOwnerSubscriptionAsync(int ownerUserId);
    Task<OwnerSubscriptionDto> RequestSubscriptionAsync(int ownerUserId, int packageId);
    Task<SubscriptionPaymentCheckoutDto?> GetPaymentCheckoutAsync(int ownerUserId);
    Task<bool> ConfirmPaymentFromWebhookAsync(string transferContent, decimal amount, string? externalTransactionId);
    Task<bool> SimulatePaymentAsync(int ownerUserId);
}
