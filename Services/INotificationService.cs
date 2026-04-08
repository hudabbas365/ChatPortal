using ChatPortal.Models.Entities;

namespace ChatPortal.Services;

public record NotificationDto(Guid Id, string Title, string Content, string Priority, bool IsRead, bool IsDismissed, string? ActionUrl, DateTime CreatedAt);
public record AnnouncementDto(Guid Id, string Title, string Content, string Priority, bool IsRead, bool IsDismissed, DateTime CreatedAt);

public interface INotificationService
{
    Task<List<NotificationDto>> GetUserNotificationsAsync(Guid userId, bool includeRead = true, bool includeDismissed = false);
    Task<List<AnnouncementDto>> GetUserAnnouncementsAsync(Guid userId);
    Task<int> GetUnreadCountAsync(Guid userId);
    Task MarkNotificationAsReadAsync(Guid notificationId, Guid userId);
    Task MarkAllNotificationsAsReadAsync(Guid userId);
    Task DismissNotificationAsync(Guid notificationId, Guid userId);
    Task MarkAnnouncementAsReadAsync(Guid announcementId, Guid userId);
    Task DismissAnnouncementAsync(Guid announcementId, Guid userId);
    Task<Announcement?> CreateAnnouncementAsync(string title, string content, AnnouncementPriority priority, Guid adminUserId, DateTime? expiresAt = null);
    Task<bool> UpdateAnnouncementAsync(Guid id, string title, string content, AnnouncementPriority priority, bool isActive, DateTime? expiresAt);
    Task<bool> DeleteAnnouncementAsync(Guid id);
    Task<List<Announcement>> GetAllAnnouncementsAsync();
    Task CreateNotificationForUserAsync(Guid userId, string title, string content, NotificationPriority priority, string? actionUrl = null);
}
