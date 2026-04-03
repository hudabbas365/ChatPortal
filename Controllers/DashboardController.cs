using ChatPortal.Data;
using ChatPortal.Services;
using ChatPortal.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ChatPortal.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly AppDbContext _context;
    private readonly ICreditService _creditService;

    public DashboardController(AppDbContext context, ICreditService creditService)
    {
        _context = context;
        _creditService = creditService;
    }

    private int GetUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    public async Task<IActionResult> Index()
    {
        var userId = GetUserId();

        var user = await _context.Users.FindAsync(userId);
        var userName = user != null ? $"{user.FirstName} {user.LastName}".Trim() : "User";

        var totalChats = await _context.ChatSessions.CountAsync(c => c.UserId == userId);
        var creditsBalance = await _creditService.GetBalanceAsync(userId);

        var subscription = await _context.Subscriptions
            .Include(s => s.Plan)
            .Where(s => s.UserId == userId && s.Status == "Active")
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefaultAsync();

        var planName = subscription?.Plan.Name ?? "Free";
        var maxCredits = subscription?.Plan.MaxCredits ?? 100;
        var creditsUsed = Math.Max(0, maxCredits - creditsBalance);

        var apiCallCount = await _context.QueryHistories.CountAsync(q => q.UserId == userId);

        var savedSessions = await _context.ChatSessions
            .CountAsync(c => c.UserId == userId);

        var recentQueryHistories = await _context.QueryHistories
            .Where(q => q.UserId == userId)
            .OrderByDescending(q => q.ExecutedAt)
            .Take(4)
            .ToListAsync();

        var recentActivity = recentQueryHistories.Select(q => new RecentActivityItem
        {
            Action = $"Ran query on data source #{q.DataSourceId}",
            Time = GetRelativeTime(q.ExecutedAt),
            Icon = "bi-database"
        }).ToList();

        if (!recentActivity.Any())
        {
            recentActivity = new List<RecentActivityItem>
            {
                new() { Action = "No recent activity yet", Time = "", Icon = "bi-clock" }
            };
        }

        var vm = new DashboardViewModel
        {
            UserName = userName,
            TotalChats = totalChats,
            CreditsUsed = creditsUsed,
            CreditsRemaining = creditsBalance,
            PlanName = planName,
            Stats = new List<QuickStatItem>
            {
                new() { Label = "Total Chats", Value = totalChats.ToString("N0"), Icon = "bi-chat-dots", Color = "primary" },
                new() { Label = "Credits Balance", Value = creditsBalance.ToString("N0"), Icon = "bi-lightning", Color = "warning" },
                new() { Label = "API Calls", Value = apiCallCount.ToString("N0"), Icon = "bi-code-square", Color = "info" },
                new() { Label = "Saved Sessions", Value = savedSessions.ToString("N0"), Icon = "bi-bookmark", Color = "success" }
            },
            RecentActivity = recentActivity
        };

        return View(vm);
    }

    private static string GetRelativeTime(DateTime dt)
    {
        var diff = DateTime.UtcNow - dt;
        if (diff.TotalMinutes < 1) return "Just now";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} minutes ago";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} hours ago";
        if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} days ago";
        return dt.ToString("MMM dd, yyyy");
    }
}
