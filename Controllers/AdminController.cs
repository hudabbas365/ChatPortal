using ChatPortal.Data;
using ChatPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatPortal.Controllers;

[Authorize]
public class AdminController : Controller
{
    private readonly INotificationService _notificationService;
    private readonly AppDbContext _db;

    public AdminController(INotificationService notificationService, AppDbContext db)
    {
        _notificationService = notificationService;
        _db = db;
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return claim != null ? int.Parse(claim.Value) : 0;
    }

    public IActionResult Index()
    {
        ViewBag.TotalUsers = 1247;
        ViewBag.ActiveSubscriptions = 342;
        ViewBag.TotalRevenue = "$18,432";
        ViewBag.ApiCalls = "48,291";
        return View();
    }

    // ── Announcements ──────────────────────────────────────────────────────────

    public async Task<IActionResult> Announcements()
    {
        var announcements = await _notificationService.GetAllAnnouncementsAsync();
        return View(announcements);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAnnouncement(string title, string content, string priority)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
        {
            TempData["Error"] = "Title and content are required.";
            return RedirectToAction(nameof(Announcements));
        }

        await _notificationService.CreateAnnouncementAsync(title, content, priority ?? "informational", GetUserId());
        TempData["Success"] = "Announcement created successfully.";
        return RedirectToAction(nameof(Announcements));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAnnouncement(int id, string title, string content, string priority, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
        {
            TempData["Error"] = "Title and content are required.";
            return RedirectToAction(nameof(Announcements));
        }

        await _notificationService.UpdateAnnouncementAsync(id, title, content, priority ?? "informational", isActive);
        TempData["Success"] = "Announcement updated successfully.";
        return RedirectToAction(nameof(Announcements));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAnnouncement(int id)
    {
        await _notificationService.DeleteAnnouncementAsync(id);
        TempData["Success"] = "Announcement deleted.";
        return RedirectToAction(nameof(Announcements));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BroadcastAnnouncement(int id)
    {
        // Broadcast to all users in the system (in-memory demo users + any DB users)
        var userIds = _db.Users.Select(u => u.Id).ToList();
        // Also include the standard in-memory demo user IDs
        var allIds = userIds.Union(new[] { 1, 2 }).Distinct();
        await _notificationService.BroadcastAnnouncementAsync(id, allIds);
        TempData["Success"] = "Announcement broadcast to all users.";
        return RedirectToAction(nameof(Announcements));
    }
}

