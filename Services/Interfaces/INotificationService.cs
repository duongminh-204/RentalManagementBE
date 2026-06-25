using Backend.DTOs.Legal;

namespace Backend.Services.Interfaces;

public interface INotificationService
{
    Task<List<NotificationDto>> GetForUserAsync(int userId, bool? unreadOnly = null, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(int userId, CancellationToken ct = default);
    Task<bool> MarkAsReadAsync(int notificationId, int userId, CancellationToken ct = default);
    Task<int> MarkAllAsReadAsync(int userId, CancellationToken ct = default);
    Task CreateIfNotExistsAsync(int userId, string type, string title, string content, CancellationToken ct = default);
}
