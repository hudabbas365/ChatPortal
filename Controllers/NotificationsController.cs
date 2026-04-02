using ChatPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatPortal.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return claim != null ? int.Parse(claim.Value) : 0;
    }

    // GET /Notifications
    public async Task<IActionResult> Index()
    {
        var userId = GetUserId();
        var notifications = await _notificationService.GetForUserAsync(userId);
        ViewBag.UnreadCount = notifications.Count(n => !n.IsRead);
        return View(notifications);
    }

    // POST /Notifications/MarkAsRead
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        await _notificationService.MarkAsReadAsync(id, GetUserId());
        return RedirectToAction(nameof(Index));
    }

    // POST /Notifications/MarkAsUnread
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsUnread(int id)
    {
        await _notificationService.MarkAsUnreadAsync(id, GetUserId());
        return RedirectToAction(nameof(Index));
    }

    // POST /Notifications/Dismiss
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Dismiss(int id)
    {
        await _notificationService.DismissAsync(id, GetUserId());
        return RedirectToAction(nameof(Index));
    }

    // GET /Notifications/GetDropdown (partial for AJAX refresh)
    public async Task<IActionResult> GetDropdown()
    {
        var userId = GetUserId();
        var notifications = await _notificationService.GetForUserAsync(userId);
        return PartialView("_NotificationDropdown", notifications);
    }
}
