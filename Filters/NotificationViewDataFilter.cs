using ChatPortal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ChatPortal.Filters;

public class NotificationViewDataFilter : IAsyncActionFilter
{
    private readonly INotificationService _notificationService;

    public NotificationViewDataFilter(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executed = await next();

        if (executed.Controller is Controller controller)
        {
            // Always populate active announcements (shown as banners)
            var announcements = await _notificationService.GetActiveAnnouncementsAsync();
            controller.ViewBag.ActiveAnnouncements = announcements;

            // Populate user notifications if authenticated
            var userIdClaim = controller.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            {
                var notifications = await _notificationService.GetForUserAsync(userId);
                controller.ViewBag.UserNotifications = notifications;
            }
        }
    }
}
