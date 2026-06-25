using Backend.Authorization;
using Backend.DTOs.Package;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[Route("api/subscriptions")]
[ApiController]
[Authorize(Policy = AuthorizationPolicies.ActiveOwner)]
public class OwnerSubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;

    public OwnerSubscriptionsController(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
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
}
