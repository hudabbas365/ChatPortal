using ChatPortal.Models.Entities;

namespace ChatPortal.Services;

public interface INotificationService
{
    Task<List<Notification>> GetForUserAsync(int userId, bool includeRead = true);
    Task<int> GetUnreadCountAsync(int userId);
    Task MarkAsReadAsync(int notificationId, int userId);
    Task MarkAsUnreadAsync(int notificationId, int userId);
    Task DismissAsync(int notificationId, int userId);
    Task<Notification> CreateAsync(int userId, string title, string content, string priority = "informational", string? actionUrl = null);

    Task<List<GlobalAnnouncement>> GetActiveAnnouncementsAsync();
    Task<List<GlobalAnnouncement>> GetAllAnnouncementsAsync();
    Task<GlobalAnnouncement> CreateAnnouncementAsync(string title, string content, string priority, int createdById);
    Task UpdateAnnouncementAsync(int id, string title, string content, string priority, bool isActive);
    Task DeleteAnnouncementAsync(int id);
    Task BroadcastAnnouncementAsync(int announcementId, IEnumerable<int> userIds);
}
