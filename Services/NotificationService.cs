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

    public async Task<List<NotificationDto>> GetUserNotificationsAsync(int userId, bool includeRead = true, bool includeDismissed = false)
    {
        var query = _db.Notifications.Where(n => n.UserId == userId && !n.IsDismissed);
        if (!includeRead) query = query.Where(n => !n.IsRead);
        if (!includeDismissed) query = query.Where(n => !n.IsDismissed);
        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto(n.Id, n.Title, n.Content, n.Priority.ToString(), n.IsRead, n.IsDismissed, n.ActionUrl, n.CreatedAt))
            .ToListAsync();
    }

    public async Task<List<AnnouncementDto>> GetUserAnnouncementsAsync(int userId)
    {
        var now = DateTime.UtcNow;
        var activeAnnouncements = await _db.Announcements
            .Where(a => a.IsActive && (a.ExpiresAt == null || a.ExpiresAt > now))
            .ToListAsync();

        var userStatuses = await _db.AnnouncementReadStatuses
            .Where(s => s.UserId == userId)
            .ToListAsync();

        return activeAnnouncements
            .Where(a => userStatuses.All(s => s.AnnouncementId != a.Id || !s.IsDismissed))
            .OrderByDescending(a => a.CreatedAt)
            .Select(a =>
            {
                var status = userStatuses.FirstOrDefault(s => s.AnnouncementId == a.Id);
                return new AnnouncementDto(a.Id, a.Title, a.Content, a.Priority.ToString(), status?.IsRead ?? false, status?.IsDismissed ?? false, a.CreatedAt);
            })
            .ToList();
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        var unreadNotifications = await _db.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead && !n.IsDismissed);

        var now = DateTime.UtcNow;
        var activeAnnouncementIds = await _db.Announcements
            .Where(a => a.IsActive && (a.ExpiresAt == null || a.ExpiresAt > now))
            .Select(a => a.Id)
            .ToListAsync();

        var readOrDismissedAnnouncementIds = await _db.AnnouncementReadStatuses
            .Where(s => s.UserId == userId && (s.IsRead || s.IsDismissed))
            .Select(s => s.AnnouncementId)
            .ToListAsync();

        var unreadAnnouncements = activeAnnouncementIds.Count(id => !readOrDismissedAnnouncementIds.Contains(id));

        return unreadNotifications + unreadAnnouncements;
    }

    public async Task MarkNotificationAsReadAsync(int notificationId, int userId)
    {
        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == notificationId && x.UserId == userId);
        if (n != null)
        {
            n.IsRead = true;
            await _db.SaveChangesAsync();
        }
    }

    public async Task MarkAllNotificationsAsReadAsync(int userId)
    {
        var notifications = await _db.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
        foreach (var n in notifications) n.IsRead = true;
        await _db.SaveChangesAsync();
    }

    public async Task DismissNotificationAsync(int notificationId, int userId)
    {
        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == notificationId && x.UserId == userId);
        if (n != null)
        {
            n.IsDismissed = true;
            n.IsRead = true;
            await _db.SaveChangesAsync();
        }
    }

    public async Task MarkAnnouncementAsReadAsync(int announcementId, int userId)
    {
        var status = await _db.AnnouncementReadStatuses.FirstOrDefaultAsync(s => s.AnnouncementId == announcementId && s.UserId == userId);
        if (status == null)
        {
            _db.AnnouncementReadStatuses.Add(new AnnouncementReadStatus { AnnouncementId = announcementId, UserId = userId, IsRead = true, ReadAt = DateTime.UtcNow });
        }
        else
        {
            status.IsRead = true;
            status.ReadAt ??= DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
    }

    public async Task DismissAnnouncementAsync(int announcementId, int userId)
    {
        var status = await _db.AnnouncementReadStatuses.FirstOrDefaultAsync(s => s.AnnouncementId == announcementId && s.UserId == userId);
        if (status == null)
        {
            _db.AnnouncementReadStatuses.Add(new AnnouncementReadStatus { AnnouncementId = announcementId, UserId = userId, IsRead = true, IsDismissed = true, ReadAt = DateTime.UtcNow });
        }
        else
        {
            status.IsDismissed = true;
            status.IsRead = true;
            status.ReadAt ??= DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
    }

    public async Task<Announcement?> CreateAnnouncementAsync(string title, string content, AnnouncementPriority priority, int adminUserId, DateTime? expiresAt = null)
    {
        var announcement = new Announcement
        {
            Title = title,
            Content = content,
            Priority = priority,
            CreatedByAdminId = adminUserId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt
        };
        _db.Announcements.Add(announcement);
        await _db.SaveChangesAsync();
        return announcement;
    }

    public async Task<bool> UpdateAnnouncementAsync(int id, string title, string content, AnnouncementPriority priority, bool isActive, DateTime? expiresAt)
    {
        var announcement = await _db.Announcements.FindAsync(id);
        if (announcement == null) return false;
        announcement.Title = title;
        announcement.Content = content;
        announcement.Priority = priority;
        announcement.IsActive = isActive;
        announcement.ExpiresAt = expiresAt;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAnnouncementAsync(int id)
    {
        var announcement = await _db.Announcements.FindAsync(id);
        if (announcement == null) return false;
        _db.Announcements.Remove(announcement);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<Announcement>> GetAllAnnouncementsAsync()
    {
        return await _db.Announcements
            .Include(a => a.CreatedByAdmin)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task CreateNotificationForUserAsync(int userId, string title, string content, NotificationPriority priority, string? actionUrl = null)
    {
        _db.Notifications.Add(new Notification
        {
            UserId = userId,
            Title = title,
            Content = content,
            Priority = priority,
            ActionUrl = actionUrl,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }
}
