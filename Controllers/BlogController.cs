using Microsoft.AspNetCore.Mvc;

namespace ChatPortal.Controllers;

public class BlogController : Controller
{
    private static readonly List<(int Id, string Title, string Slug, string Author, string Excerpt, string Date, string Tag)> _posts = new()
    {
        (1, "Getting Started with ChatPortal", "getting-started", "Team ChatPortal", "Learn how to get the most out of ChatPortal's AI features.", "Jan 15, 2025", "Guide"),
        (2, "Understanding AI Models", "understanding-ai-models", "Dr. Sarah Chen", "A deep dive into the differences between GPT models.", "Jan 10, 2025", "AI"),
        (3, "10 Productivity Tips with AI", "productivity-tips", "Mark Williams", "Boost your workflow with these AI-powered productivity hacks.", "Jan 5, 2025", "Tips"),
        (4, "API Integration Best Practices", "api-integration", "Dev Team", "How to integrate ChatPortal API into your applications.", "Dec 28, 2024", "Development")
    };

    public IActionResult Index()
    {
        ViewBag.Posts = _posts;
        return View();
    }

    public IActionResult Detail(string slug)
    {
        var post = _posts.FirstOrDefault(p => p.Slug == slug);
        if (post == default) return NotFound();
        ViewBag.Post = post;
        return View();
    }
}
