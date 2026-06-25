namespace Backend.DTOs.Package;

public class PublicPackageDto
{
    public int PackageId { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public string RoomRange { get; set; } = string.Empty;
    public string TargetAudience { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int MaxRooms { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool Recommended { get; set; }
    public List<string> Features { get; set; } = [];
}

public class OwnerSubscriptionDto
{
    public int? SubscriptionId { get; set; }
    public int? PackageId { get; set; }
    public string? PackageName { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<string> Features { get; set; } = [];
    public string? PaymentReference { get; set; }
    public decimal? Price { get; set; }
    public decimal? PaymentAmount { get; set; }
    public bool IsUpgrade { get; set; }
    public bool HasPendingUpgrade { get; set; }
    public int? PendingPackageId { get; set; }
    public string? PendingPackageName { get; set; }
    public decimal? PendingPaymentAmount { get; set; }
    public bool HasTrialAccess { get; set; }
    public List<string> EffectiveFeatures { get; set; } = [];
}

public class SubscriptionPaymentCheckoutDto
{
    public int SubscriptionId { get; set; }
    public int PackageId { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentReference { get; set; } = string.Empty;
    public string TransferContent { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankId { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public bool IsPaymentConfigured { get; set; }
    public bool IsUpgrade { get; set; }
    public string? CurrentPackageName { get; set; }
    public decimal? FullPackagePrice { get; set; }
}

public class PlatformPaymentSettingDto
{
    public string BankName { get; set; } = string.Empty;
    public string BankId { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public bool IsConfigured { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class UpdatePlatformPaymentSettingDto
{
    public string BankName { get; set; } = string.Empty;
    public string BankId { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
}

public class RequestSubscriptionDto
{
    public int PackageId { get; set; }
}
