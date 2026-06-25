using Backend.Entities;

namespace Backend.Repositories.Interfaces;

public interface INotificationRepository
{
    Task<List<Notification>> ListForUserAsync(int userId, bool? unreadOnly, CancellationToken ct = default);
    Task<int> CountUnreadAsync(int userId, CancellationToken ct = default);
    Task<Notification?> GetByIdForUserAsync(int notificationId, int userId, CancellationToken ct = default);
    Task<bool> ExistsRecentAsync(int userId, string type, string title, TimeSpan within, CancellationToken ct = default);
    void Add(Notification notification);
    Task<int> MarkAllUnreadAsReadAsync(int userId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
