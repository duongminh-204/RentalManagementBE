using Backend.Authorization;
using Backend.Configuration;
using Backend.DTOs.Package;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Backend.Controllers;

[Route("api/subscriptions")]
[ApiController]
[Authorize(Policy = AuthorizationPolicies.OwnerOnly)]
public class OwnerSubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly IWebHostEnvironment _environment;
    private readonly BankWebhookOptions _webhookOptions;

    public OwnerSubscriptionsController(
        ISubscriptionService subscriptionService,
        IWebHostEnvironment environment,
        Microsoft.Extensions.Options.IOptions<BankWebhookOptions> webhookOptions)
    {
        _subscriptionService = subscriptionService;
        _environment = environment;
        _webhookOptions = webhookOptions.Value;
    }

    [HttpGet("me")]
    public async Task<ActionResult<OwnerSubscriptionDto>> GetMySubscription()
    {
        if (!AuthorizationClaimExtensions.GetUserId(User).HasValue)
            return Unauthorized();

        var ownerUserId = AuthorizationClaimExtensions.GetUserId(User)!.Value;
        var subscription = await _subscriptionService.GetOwnerSubscriptionAsync(ownerUserId);
        return Ok(subscription ?? new OwnerSubscriptionDto { Status = "None" });
    }

    [HttpGet("payment-checkout")]
    public async Task<ActionResult<SubscriptionPaymentCheckoutDto>> GetPaymentCheckout()
    {
        if (!AuthorizationClaimExtensions.GetUserId(User).HasValue)
            return Unauthorized();

        var ownerUserId = AuthorizationClaimExtensions.GetUserId(User)!.Value;
        var checkout = await _subscriptionService.GetPaymentCheckoutAsync(ownerUserId);
        if (checkout == null)
            return NotFound(new { message = "Không có gói đang chờ thanh toán." });

        return Ok(checkout);
    }

    [HttpPost("request")]
    public async Task<ActionResult<OwnerSubscriptionDto>> RequestSubscription([FromBody] RequestSubscriptionDto dto)
    {
        if (!AuthorizationClaimExtensions.GetUserId(User).HasValue)
            return Unauthorized();

        try
        {
            var ownerUserId = AuthorizationClaimExtensions.GetUserId(User)!.Value;
            var subscription = await _subscriptionService.RequestSubscriptionAsync(ownerUserId, dto.PackageId);
            return Ok(subscription);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Chỉ dùng khi BankWebhook:AllowDevSimulate = true (môi trường dev).</summary>
    [HttpPost("dev/simulate-payment")]
    public async Task<IActionResult> SimulatePayment()
    {
        if (!_environment.IsDevelopment() && !_webhookOptions.AllowDevSimulate)
            return NotFound();

        if (!AuthorizationClaimExtensions.GetUserId(User).HasValue)
            return Unauthorized();

        var ownerUserId = AuthorizationClaimExtensions.GetUserId(User)!.Value;
        var success = await _subscriptionService.SimulatePaymentAsync(ownerUserId);
        if (!success)
            return BadRequest(new { message = "Không có gói đang chờ thanh toán." });

        var subscription = await _subscriptionService.GetOwnerSubscriptionAsync(ownerUserId);
        return Ok(subscription);
    }
}
