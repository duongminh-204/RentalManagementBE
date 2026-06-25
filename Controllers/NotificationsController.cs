using Backend.Authorization;
using Backend.DTOs.Legal;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Controllers;

[Route("api/notifications")]
[ApiController]
[Authorize(Policy = AuthorizationPolicies.ActiveOwner)]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notifications;

    public NotificationsController(INotificationService notifications)
    {
        _notifications = notifications;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<NotificationDto>>> GetAll(
        [FromQuery] bool? unreadOnly, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok(await _notifications.GetForUserAsync(userId, unreadOnly, ct));
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<object>> GetUnreadCount(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var count = await _notifications.GetUnreadCountAsync(userId, ct);
        return Ok(new { count });
    }

    [HttpPatch("{id:int}/read")]
    public async Task<IActionResult> MarkAsRead(int id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return await _notifications.MarkAsReadAsync(id, userId, ct) ? NoContent() : NotFound();
    }

    [HttpPost("read-all")]
    public async Task<ActionResult<object>> MarkAllAsRead(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var count = await _notifications.MarkAllAsReadAsync(userId, ct);
        return Ok(new { count });
    }

    private bool TryGetUserId(out int userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out userId);
    }
}
