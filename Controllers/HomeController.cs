using Microsoft.AspNetCore.Mvc;

namespace ChatPortal.Controllers;

/// <summary>
/// Handles the public home page of the ChatPortal application.
/// </summary>
public class HomeController : Controller
{
    /// <summary>
    /// Renders the application home page and sets SEO-optimised ViewData values
    /// for the page title, meta description, and keywords.
    /// </summary>
    /// <returns>The Home/Index Razor view.</returns>
    public IActionResult Index()
    {
        ViewData["Title"] = "ChatPortal — AI-Powered Chat Platform";
        ViewData["MetaDescription"] = "ChatPortal is an AI-powered chat platform for individuals and teams. Chat with GPT-4, analyze your data, embed AI in your apps, and export conversations as PDF.";
        ViewData["MetaKeywords"] = "AI chat, ChatPortal, GPT-4, OpenAI, AI assistant, chat platform, code help, data analysis";
        return View();
    }
}
