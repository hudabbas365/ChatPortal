using ChatPortal.Data;
using ChatPortal.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatPortal.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;

    public NotificationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Notification>> GetForUserAsync(int userId, bool includeRead = true)
    {
        var query = _db.Notifications
            .Where(n => n.UserId == userId && n.DismissedAt == null);

        if (!includeRead)
            query = query.Where(n => !n.IsRead);

        return await query.OrderByDescending(n => n.CreatedAt).ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _db.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead && n.DismissedAt == null);
    }

    public async Task MarkAsReadAsync(int notificationId, int userId)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
        if (notification == null) return;

        notification.IsRead = true;
        await _db.SaveChangesAsync();
    }

    public async Task MarkAsUnreadAsync(int notificationId, int userId)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
        if (notification == null) return;

        notification.IsRead = false;
        await _db.SaveChangesAsync();
    }

    public async Task DismissAsync(int notificationId, int userId)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
        if (notification == null) return;

        notification.DismissedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<Notification> CreateAsync(int userId, string title, string content,
        string priority = "informational", string? actionUrl = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Content = content,
            Priority = priority,
            ActionUrl = actionUrl,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();
        return notification;
    }

    // ── Announcements ──────────────────────────────────────────────────────────

    public async Task<List<GlobalAnnouncement>> GetActiveAnnouncementsAsync()
    {
        return await _db.GlobalAnnouncements
            .Where(a => a.IsActive)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<GlobalAnnouncement>> GetAllAnnouncementsAsync()
    {
        return await _db.GlobalAnnouncements
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<GlobalAnnouncement> CreateAnnouncementAsync(string title, string content,
        string priority, int createdById)
    {
        var announcement = new GlobalAnnouncement
        {
            Title = title,
            Content = content,
            Priority = priority,
            IsActive = true,
            CreatedById = createdById,
            CreatedAt = DateTime.UtcNow
        };
        _db.GlobalAnnouncements.Add(announcement);
        await _db.SaveChangesAsync();
        return announcement;
    }

    public async Task UpdateAnnouncementAsync(int id, string title, string content,
        string priority, bool isActive)
    {
        var announcement = await _db.GlobalAnnouncements.FindAsync(id);
        if (announcement == null) return;

        announcement.Title = title;
        announcement.Content = content;
        announcement.Priority = priority;
        announcement.IsActive = isActive;
        announcement.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAnnouncementAsync(int id)
    {
        var announcement = await _db.GlobalAnnouncements.FindAsync(id);
        if (announcement == null) return;

        _db.GlobalAnnouncements.Remove(announcement);
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Creates a personal Notification for every user derived from a GlobalAnnouncement.
    /// </summary>
    public async Task BroadcastAnnouncementAsync(int announcementId, IEnumerable<int> userIds)
    {
        var announcement = await _db.GlobalAnnouncements.FindAsync(announcementId);
        if (announcement == null) return;

        var notifications = userIds.Select(uid => new Notification
        {
            UserId = uid,
            Title = announcement.Title,
            Content = announcement.Content,
            Priority = announcement.Priority,
            Type = "announcement",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        _db.Notifications.AddRange(notifications);
        await _db.SaveChangesAsync();
    }
}
