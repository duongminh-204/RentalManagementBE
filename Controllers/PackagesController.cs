using Backend.DTOs.Package;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[Route("api/packages")]
[ApiController]
public class PackagesController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;

    public PackagesController(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    [HttpGet("public")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<PublicPackageDto>>> GetPublicPackages()
        => Ok(await _subscriptionService.GetPublicPackagesAsync());
}
