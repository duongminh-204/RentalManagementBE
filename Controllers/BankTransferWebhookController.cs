using Backend.Authorization;
using Backend.Configuration;
using Backend.DTOs.Package;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Backend.Controllers;

[Route("api/webhooks")]
[ApiController]
public class BankTransferWebhookController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly BankWebhookOptions _options;
    private readonly ILogger<BankTransferWebhookController> _logger;

    public BankTransferWebhookController(
        ISubscriptionService subscriptionService,
        IOptions<BankWebhookOptions> options,
        ILogger<BankTransferWebhookController> logger)
    {
        _subscriptionService = subscriptionService;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Webhook nhận biến động số dư từ SePay hoặc Casso.
    /// Cấu hình URL: POST /api/webhooks/bank-transfer
    /// </summary>
    [HttpPost("bank-transfer")]
    public async Task<IActionResult> HandleBankTransfer()
    {
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            var authHeader = Request.Headers.Authorization.ToString();
            var apiKeyHeader = Request.Headers["X-Api-Key"].ToString();
            var expected = $"Apikey {_options.ApiKey}";

            var authorized =
                string.Equals(authHeader, expected, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(apiKeyHeader, _options.ApiKey, StringComparison.Ordinal);

            if (!authorized)
                return Unauthorized();
        }

        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(body))
            return BadRequest(new { message = "Empty body" });

        try
        {
            var (content, amount, transactionId) = ParseWebhookPayload(body);
            if (string.IsNullOrWhiteSpace(content) || amount <= 0)
            {
                _logger.LogWarning("Webhook ignored: missing content or amount. Body={Body}", body);
                return Ok(new { success = true, message = "Ignored" });
            }

            var confirmed = await _subscriptionService.ConfirmPaymentFromWebhookAsync(
                content,
                amount,
                transactionId);

            _logger.LogInformation(
                "Bank webhook processed: tx={TxId}, amount={Amount}, confirmed={Confirmed}",
                transactionId,
                amount,
                confirmed);

            return Ok(new { success = true, confirmed });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid webhook JSON: {Body}", body);
            return BadRequest(new { message = "Invalid JSON" });
        }
    }

    private static (string content, decimal amount, string? transactionId) ParseWebhookPayload(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        // SePay format
        if (root.TryGetProperty("content", out var sepayContent) &&
            root.TryGetProperty("transferAmount", out var sepayAmount))
        {
            var txId = root.TryGetProperty("id", out var sepayId)
                ? $"SEPAY-{sepayId.GetRawText()}"
                : null;
            return (sepayContent.GetString() ?? string.Empty, sepayAmount.GetDecimal(), txId);
        }

        // Casso format: { "data": [{ "description", "amount", "id" }] }
        if (root.TryGetProperty("data", out var cassoData) && cassoData.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in cassoData.EnumerateArray())
            {
                var description = item.TryGetProperty("description", out var desc)
                    ? desc.GetString() ?? string.Empty
                    : string.Empty;
                var amount = item.TryGetProperty("amount", out var amt) ? amt.GetDecimal() : 0m;
                var txId = item.TryGetProperty("id", out var id)
                    ? $"CASSO-{id.GetRawText()}"
                    : null;
                if (!string.IsNullOrWhiteSpace(description) && amount > 0)
                    return (description, amount, txId);
            }
        }

        // Generic fallback
        var genericContent = root.TryGetProperty("description", out var genericDesc)
            ? genericDesc.GetString() ?? string.Empty
            : root.TryGetProperty("transferContent", out var genericTransfer)
                ? genericTransfer.GetString() ?? string.Empty
                : string.Empty;
        var genericAmount = root.TryGetProperty("amount", out var genericAmt) ? genericAmt.GetDecimal() : 0m;
        var genericTxId = root.TryGetProperty("transactionId", out var genericTx)
            ? genericTx.GetString()
            : null;

        return (genericContent, genericAmount, genericTxId);
    }
}
