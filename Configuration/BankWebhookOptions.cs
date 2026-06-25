namespace Backend.Configuration;

public class BankWebhookOptions
{
    public const string SectionName = "BankWebhook";

    /// <summary>API key SePay gửi trong header Authorization: Apikey {key}.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Cho phép endpoint mô phỏng thanh toán trong Development.</summary>
    public bool AllowDevSimulate { get; set; }
}
