using Backend.DTOs.Legal;
using Backend.Entities;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;

namespace Backend.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notifications;

    public NotificationService(INotificationRepository notifications)
    {
        _notifications = notifications;
    }

    public async Task<List<NotificationDto>> GetForUserAsync(int userId, bool? unreadOnly = null, CancellationToken ct = default)
    {
        var list = await _notifications.ListForUserAsync(userId, unreadOnly, ct);
        return list.Select(Map).ToList();
    }

    public Task<int> GetUnreadCountAsync(int userId, CancellationToken ct = default) =>
        _notifications.CountUnreadAsync(userId, ct);

    public async Task<bool> MarkAsReadAsync(int notificationId, int userId, CancellationToken ct = default)
    {
        var notification = await _notifications.GetByIdForUserAsync(notificationId, userId, ct);
        if (notification == null) return false;

        notification.IsRead = true;
        await _notifications.SaveChangesAsync(ct);
        return true;
    }

    public async Task<int> MarkAllAsReadAsync(int userId, CancellationToken ct = default) =>
        await _notifications.MarkAllUnreadAsReadAsync(userId, ct);

    public async Task CreateIfNotExistsAsync(int userId, string type, string title, string content, CancellationToken ct = default)
    {
        if (await _notifications.ExistsRecentAsync(userId, type, title, TimeSpan.FromHours(24), ct))
            return;

        _notifications.Add(new Notification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Content = content,
            IsRead = false,
            CreatedAt = DateTime.Now
        });
        await _notifications.SaveChangesAsync(ct);
    }

    private static NotificationDto Map(Notification n) => new()
    {
        Id = n.NotificationId,
        Title = n.Title,
        Content = n.Content,
        Type = n.Type,
        IsRead = n.IsRead,
        CreatedAt = n.CreatedAt
    };
}
