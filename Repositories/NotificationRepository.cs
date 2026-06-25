using Backend.Data;
using Backend.Entities;
using Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly RentalManagementDb _db;

    public NotificationRepository(RentalManagementDb db)
    {
        _db = db;
    }

    public async Task<List<Notification>> ListForUserAsync(int userId, bool? unreadOnly, CancellationToken ct = default)
    {
        var query = _db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId);

        if (unreadOnly == true)
            query = query.Where(n => !n.IsRead);

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(100)
            .ToListAsync(ct);
    }

    public async Task<int> CountUnreadAsync(int userId, CancellationToken ct = default) =>
        await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, ct);

    public async Task<Notification?> GetByIdForUserAsync(int notificationId, int userId, CancellationToken ct = default) =>
        await _db.Notifications.FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId, ct);

    public async Task<bool> ExistsRecentAsync(int userId, string type, string title, TimeSpan within, CancellationToken ct = default)
    {
        var since = DateTime.Now - within;
        return await _db.Notifications.AnyAsync(n =>
            n.UserId == userId &&
            n.Type == type &&
            n.Title == title &&
            n.CreatedAt >= since, ct);
    }

    public void Add(Notification notification) => _db.Notifications.Add(notification);

    public async Task<int> MarkAllUnreadAsReadAsync(int userId, CancellationToken ct = default)
    {
        var unread = await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(ct);

        foreach (var n in unread)
            n.IsRead = true;

        await _db.SaveChangesAsync(ct);
        return unread.Count;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
