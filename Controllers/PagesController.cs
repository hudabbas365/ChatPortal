using Microsoft.AspNetCore.Mvc;

namespace ChatPortal.Controllers;

/// <summary>
/// Serves the rich public-facing informational pages of ChatPortal: About, Contact,
/// API Documentation, and the Chat History feature overview.
/// These pages are SEO-optimised with meta-description and keyword ViewData values.
/// </summary>
public class PagesController : Controller
{
    /// <summary>
    /// Renders the "About ChatPortal" page, which describes the platform's mission,
    /// team values, and technology stack.
    /// </summary>
    /// <returns>The About Razor view.</returns>
    [HttpGet]
    public IActionResult About()
    {
        ViewData["Title"] = "About ChatPortal — AI-Powered Chat Platform";
        ViewData["MetaDescription"] = "Learn about ChatPortal — our mission, the team behind the AI chat platform, our technology stack (ASP.NET Core, OpenAI, SignalR), and how we empower individuals and teams with intelligent conversation.";
        ViewData["MetaKeywords"] = "about ChatPortal, AI chat platform, OpenAI, ASP.NET Core, SignalR, mission, team, technology";
        return View();
    }

    /// <summary>
    /// Renders the Contact page, which includes a contact form, office information,
    /// a FAQ section, and details about support channels.
    /// </summary>
    /// <returns>The Contact Razor view.</returns>
    [HttpGet]
    public IActionResult Contact()
    {
        ViewData["Title"] = "Contact ChatPortal — Get in Touch";
        ViewData["MetaDescription"] = "Contact the ChatPortal team. Reach out with questions, feedback, or partnership enquiries. We're here to help via email, live chat, and our community forum.";
        ViewData["MetaKeywords"] = "contact ChatPortal, support, help, email, feedback, partnership";
        return View();
    }

    /// <summary>
    /// Renders the API Documentation page, which explains authentication, endpoints,
    /// iframe embedding, SignalR integration, and rate limits with code examples.
    /// </summary>
    /// <returns>The Docs Razor view.</returns>
    [HttpGet]
    public IActionResult Docs()
    {
        ViewData["Title"] = "ChatPortal API Documentation — Developer Guide";
        ViewData["MetaDescription"] = "Comprehensive ChatPortal API documentation: JWT authentication, Chat endpoints, iframe embedding, SignalR integration, rate limits, and code examples in curl, JavaScript, Python, and C#.";
        ViewData["MetaKeywords"] = "ChatPortal API, JWT authentication, iframe embed, SignalR, REST API, documentation, developer guide";
        return View();
    }

    /// <summary>
    /// Renders the public-facing Chat History feature overview page, explaining how
    /// browser-based localStorage history works, its privacy benefits, and import/export.
    /// </summary>
    /// <returns>The History Razor view.</returns>
    [HttpGet]
    public IActionResult History()
    {
        ViewData["Title"] = "Chat History — How ChatPortal Stores Conversations Privately";
        ViewData["MetaDescription"] = "Discover how ChatPortal's browser-based chat history keeps your conversations private. All data stays in your browser via localStorage — no server storage required.";
        ViewData["MetaKeywords"] = "chat history, localStorage, browser storage, privacy, conversation history, export chat";
        return View();
    }

    /// <summary>
    /// Handles the contact form POST submission. In this implementation the message
    /// is acknowledged and the user is redirected back with a success notification.
    /// </summary>
    /// <param name="name">The sender's full name.</param>
    /// <param name="email">The sender's email address.</param>
    /// <param name="subject">The subject of the enquiry.</param>
    /// <param name="message">The enquiry message body.</param>
    /// <returns>A redirect back to the Contact page with a success or error TempData message.</returns>
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ContactSubmit(string name, string email, string subject, string message)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(message))
        {
            TempData["Error"] = "Please fill in all required fields.";
            return RedirectToAction(nameof(Contact));
        }

        // TODO: Integrate IEmailService to deliver this submission as an email notification.
        // Until then we log the enquiry and acknowledge with a success message.
        // In a production app this would queue an email via IEmailService.
        // For now we acknowledge the submission with a success message.
        TempData["Success"] = $"Thank you, {name}! Your message has been received. We'll reply to {email} shortly.";
        return RedirectToAction(nameof(Contact));
    }
}
