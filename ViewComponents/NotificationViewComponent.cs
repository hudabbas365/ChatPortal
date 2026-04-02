using ChatPortal.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatPortal.ViewComponents;

public class NotificationViewComponent : ViewComponent
{
    private readonly INotificationService _notificationService;

    public NotificationViewComponent(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var claim = UserClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(claim, out var userId))
            return View(new NotificationViewComponentModel(0, new(), new()));

        var notifications = await _notificationService.GetUserNotificationsAsync(userId, includeRead: false);
        var announcements = await _notificationService.GetUserAnnouncementsAsync(userId);
        var unread = await _notificationService.GetUnreadCountAsync(userId);

        return View(new NotificationViewComponentModel(unread, notifications, announcements));
    }
}

public record NotificationViewComponentModel(
    int UnreadCount,
    List<NotificationDto> Notifications,
    List<AnnouncementDto> Announcements
);
