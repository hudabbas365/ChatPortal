using ChatPortal.Services;
using ChatPortal.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatPortal.Controllers;

public class DashboardController : Controller
{
    public IActionResult Index()
    {
        var vm = new DashboardViewModel
        {
            UserName = "Demo User",
            TotalChats = 24,
            CreditsUsed = 350,
            CreditsRemaining = 650,
            PlanName = "Pro",
            Stats = new List<QuickStatItem>
            {
                new() { Label = "Total Chats", Value = "24", Icon = "bi-chat-dots", Color = "primary" },
                new() { Label = "Credits Used", Value = "350", Icon = "bi-lightning", Color = "warning" },
                new() { Label = "API Calls", Value = "1,204", Icon = "bi-code-square", Color = "info" },
                new() { Label = "Saved Sessions", Value = "12", Icon = "bi-bookmark", Color = "success" }
            },
            RecentActivity = new List<RecentActivityItem>
            {
                new() { Action = "Started new chat session", Time = "2 minutes ago", Icon = "bi-chat-left-text" },
                new() { Action = "Generated code snippet", Time = "1 hour ago", Icon = "bi-code-slash" },
                new() { Action = "Exported conversation", Time = "3 hours ago", Icon = "bi-download" },
                new() { Action = "Updated profile settings", Time = "Yesterday", Icon = "bi-person-gear" }
            }
        };
        return View(vm);
    }
}
