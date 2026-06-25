using System.Text;
using System.Text.RegularExpressions;

namespace Backend.Services;

public static class SubscriptionPaymentHelper
{
    private static readonly Regex PaymentReferencePattern = new(@"DK(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string BuildPaymentReference(int subscriptionId) => $"DK{subscriptionId}";

    public static string BuildTransferContent(string paymentReference, string packageName)
    {
        var pkg = SanitizeTransferText(packageName, 20);
        return SanitizeTransferText($"{paymentReference} GOI {pkg}", 50);
    }

    public static string SanitizeTransferText(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            var category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category != System.Globalization.UnicodeCategory.NonSpacingMark)
                builder.Append(ch);
        }

        var result = Regex.Replace(builder.ToString(), @"[^a-zA-Z0-9\s]", " ")
            .Trim()
            .ToUpperInvariant();
        result = Regex.Replace(result, @"\s+", " ");

        return result.Length <= maxLength ? result : result[..maxLength];
    }

    public static int? TryExtractSubscriptionId(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;
        var match = PaymentReferencePattern.Match(content);
        return match.Success && int.TryParse(match.Groups[1].Value, out var id) ? id : null;
    }
}
