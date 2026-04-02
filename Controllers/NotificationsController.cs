using ChatPortal.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatPortal.Controllers;

public class NotificationsController : Controller
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    private int? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    public async Task<IActionResult> Index()
    {
        var userId = GetUserId();
        if (userId == null) return RedirectToAction("Login", "Account");

        var notifications = await _notificationService.GetUserNotificationsAsync(userId.Value);
        var announcements = await _notificationService.GetUserAnnouncementsAsync(userId.Value);

        ViewBag.Notifications = notifications;
        ViewBag.Announcements = announcements;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        await _notificationService.MarkNotificationAsReadAsync(id, userId.Value);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        await _notificationService.MarkAllNotificationsAsReadAsync(userId.Value);
        TempData["Success"] = "All notifications marked as read.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Dismiss(int id)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        await _notificationService.DismissNotificationAsync(id, userId.Value);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DismissAnnouncement(int id)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        await _notificationService.DismissAnnouncementAsync(id, userId.Value);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAnnouncementAsRead(int id)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        await _notificationService.MarkAnnouncementAsReadAsync(id, userId.Value);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetUserId();
        if (userId == null) return Json(new { count = 0 });
        var count = await _notificationService.GetUnreadCountAsync(userId.Value);
        return Json(new { count });
    }
}
