using ChatPortal.Models.Entities;
using ChatPortal.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatPortal.Controllers;

public class AdminController : Controller
{
    private readonly INotificationService _notificationService;

    public AdminController(INotificationService notificationService)
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
        ViewBag.TotalUsers = 1247;
        ViewBag.ActiveSubscriptions = 342;
        ViewBag.TotalRevenue = "$18,432";
        ViewBag.ApiCalls = "48,291";
        ViewBag.Announcements = await _notificationService.GetAllAnnouncementsAsync();
        return View();
    }

    // ── Announcements ────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAnnouncement(string title, string content, string priority, string? expiresAt)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        if (!Enum.TryParse<AnnouncementPriority>(priority, true, out var priorityEnum))
            priorityEnum = AnnouncementPriority.Informational;

        DateTime? expires = string.IsNullOrWhiteSpace(expiresAt) ? null : DateTime.Parse(expiresAt).ToUniversalTime();

        await _notificationService.CreateAnnouncementAsync(title, content, priorityEnum, userId.Value, expires);
        TempData["Success"] = "Announcement created and broadcasted to all users.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAnnouncement(int id, string title, string content, string priority, bool isActive, string? expiresAt)
    {
        if (!Enum.TryParse<AnnouncementPriority>(priority, true, out var priorityEnum))
            priorityEnum = AnnouncementPriority.Informational;

        DateTime? expires = string.IsNullOrWhiteSpace(expiresAt) ? null : DateTime.Parse(expiresAt).ToUniversalTime();

        var updated = await _notificationService.UpdateAnnouncementAsync(id, title, content, priorityEnum, isActive, expires);
        TempData[updated ? "Success" : "Error"] = updated ? "Announcement updated." : "Announcement not found.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAnnouncement(int id)
    {
        var deleted = await _notificationService.DeleteAnnouncementAsync(id);
        TempData[deleted ? "Success" : "Error"] = deleted ? "Announcement deleted." : "Announcement not found.";
        return RedirectToAction(nameof(Index));
    }

    // ── Embed / Dashboard Management ─────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Embeds([FromServices] IDashboardService dashboardService)
    {
        var dashboards = await dashboardService.GetAllPublicDashboardsAsync();
        return View(dashboards);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeEmbed(int id, [FromServices] IDashboardService dashboardService)
    {
        try
        {
            await dashboardService.RevokeShareAsync(id);
            TempData["Success"] = "Embed has been revoked successfully.";
        }
        catch (KeyNotFoundException)
        {
            TempData["Error"] = "Dashboard not found.";
        }
        return RedirectToAction(nameof(Embeds));
    }
}
