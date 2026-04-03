using ChatPortal.Data;
using ChatPortal.Models.Entities;
using ChatPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ChatPortal.Controllers;

[Authorize]
public class AdminController : Controller
{
    private readonly INotificationService _notificationService;
    private readonly AppDbContext _context;

    public AdminController(INotificationService notificationService, AppDbContext context)
    {
        _notificationService = notificationService;
        _context = context;
    }

    private int? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.TotalUsers = await _context.Users.CountAsync();
        ViewBag.ActiveSubscriptions = await _context.Subscriptions.CountAsync(s => s.Status == "Active");
        var totalRevenue = await _context.PaymentTransactions
            .Where(p => p.Status == "completed")
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;
        ViewBag.TotalRevenue = $"${totalRevenue:N2}";
        ViewBag.ApiCalls = (await _context.QueryHistories.CountAsync()).ToString("N0");
        ViewBag.Announcements = await _notificationService.GetAllAnnouncementsAsync();
        return View();
    }

    // ── Announcements ────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAnnouncement(string title, string content, string priority, string? expiresAt, int? organizationId)
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

    // ── Push Announcement ─────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PushAnnouncement(string title, string content, string priority, int? targetOrganizationId)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        if (!Enum.TryParse<AnnouncementPriority>(priority, true, out var priorityEnum))
            priorityEnum = AnnouncementPriority.Informational;

        if (targetOrganizationId.HasValue)
        {
            // Target specific organization members
            var memberIds = await _context.OrganizationMembers
                .Where(m => m.OrganizationId == targetOrganizationId.Value && m.IsActive)
                .Select(m => m.UserId)
                .ToListAsync();

            foreach (var memberId in memberIds)
            {
                await _notificationService.CreateNotificationForUserAsync(
                    memberId, title, content, (NotificationPriority)(int)priorityEnum);
            }
            TempData["Success"] = $"Announcement pushed to {memberIds.Count} organization members.";
        }
        else
        {
            // Global announcement
            await _notificationService.CreateAnnouncementAsync(title, content, priorityEnum, userId.Value, null);
            TempData["Success"] = "Global announcement pushed to all users.";
        }

        return RedirectToAction(nameof(Index));
    }

    // ── Organization Management ───────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> ManageOrganizations()
    {
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var orgs = await _context.Organizations
            .Include(o => o.Owner)
            .Include(o => o.Members)
            .Select(o => new
            {
                Organization = o,
                MemberCount = o.Members.Count(m => m.IsActive),
                IsInactive = o.LastActivityAt == null || o.LastActivityAt < thirtyDaysAgo
            })
            .ToListAsync();

        ViewBag.Organizations = orgs;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> OrganizationActivity(int id)
    {
        var org = await _context.Organizations
            .Include(o => o.Owner)
            .Include(o => o.Members)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (org == null)
            return NotFound();

        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var memberIds = org.Members.Select(m => m.UserId).ToList();

        var recentQueries = await _context.QueryHistories
            .Where(q => memberIds.Contains(q.UserId) && q.ExecutedAt >= thirtyDaysAgo)
            .CountAsync();

        ViewBag.Organization = org;
        ViewBag.RecentQueryCount = recentQueries;
        ViewBag.DaysSinceActivity = org.LastActivityAt.HasValue
            ? (int)(DateTime.UtcNow - org.LastActivityAt.Value).TotalDays
            : (int?)null;
        ViewBag.IsInactive = org.LastActivityAt == null || org.LastActivityAt < thirtyDaysAgo;

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RetentionAction(int organizationId, string action)
    {
        var org = await _context.Organizations.FindAsync(organizationId);
        if (org == null)
            return Json(new { success = false, error = "Organization not found" });

        switch (action.ToLower())
        {
            case "delete":
                _context.Organizations.Remove(org);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Organization '{org.Name}' has been deleted.";
                break;
            case "block":
                org.IsActive = false;
                org.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Organization '{org.Name}' has been blocked.";
                break;
            case "archive":
                org.IsActive = false;
                org.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Organization '{org.Name}' has been archived.";
                break;
            default:
                TempData["Error"] = "Invalid action.";
                break;
        }

        return RedirectToAction(nameof(ManageOrganizations));
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
