using ChatPortal.Models.Entities;

namespace ChatPortal.Services;

public record NotificationDto(int Id, string Title, string Content, string Priority, bool IsRead, bool IsDismissed, string? ActionUrl, DateTime CreatedAt);
public record AnnouncementDto(int Id, string Title, string Content, string Priority, bool IsRead, bool IsDismissed, DateTime CreatedAt);

public interface INotificationService
{
    Task<List<NotificationDto>> GetUserNotificationsAsync(int userId, bool includeRead = true, bool includeDismissed = false);
    Task<List<AnnouncementDto>> GetUserAnnouncementsAsync(int userId);
    Task<int> GetUnreadCountAsync(int userId);
    Task MarkNotificationAsReadAsync(int notificationId, int userId);
    Task MarkAllNotificationsAsReadAsync(int userId);
    Task DismissNotificationAsync(int notificationId, int userId);
    Task MarkAnnouncementAsReadAsync(int announcementId, int userId);
    Task DismissAnnouncementAsync(int announcementId, int userId);
    Task<Announcement?> CreateAnnouncementAsync(string title, string content, AnnouncementPriority priority, int adminUserId, DateTime? expiresAt = null);
    Task<bool> UpdateAnnouncementAsync(int id, string title, string content, AnnouncementPriority priority, bool isActive, DateTime? expiresAt);
    Task<bool> DeleteAnnouncementAsync(int id);
    Task<List<Announcement>> GetAllAnnouncementsAsync();
    Task CreateNotificationForUserAsync(int userId, string title, string content, NotificationPriority priority, string? actionUrl = null);
}
