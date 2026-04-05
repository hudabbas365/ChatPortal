using ChatPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatPortal.Controllers;

/// <summary>
/// Provides JWT-secured endpoints for embedding the ChatPortal chat interface
/// inside third-party websites via an HTML iframe.
/// </summary>
public class EmbedController : Controller
{
    private readonly IAIChatService _aiChatService;
    private readonly IJwtService _jwtService;

    /// <summary>
    /// Initialises a new instance of <see cref="EmbedController"/>.
    /// </summary>
    /// <param name="aiChatService">Service used to communicate with the AI backend.</param>
    /// <param name="jwtService">Service used to validate JWT tokens supplied in query strings.</param>
    public EmbedController(IAIChatService aiChatService, IJwtService jwtService)
    {
        _aiChatService = aiChatService;
        _jwtService = jwtService;
    }

    /// <summary>
    /// Renders the minimal, self-contained chat view designed for iframe embedding.
    /// Validates the JWT <paramref name="token"/> supplied in the query string before
    /// allowing access to the chat UI.
    /// </summary>
    /// <param name="token">A JWT Bearer token that authorises access to the embedded chat.</param>
    /// <returns>
    /// The embedded chat view when the token is valid; otherwise a plain-text
    /// "Unauthorised" error message suitable for display inside an iframe.
    /// </returns>
    /// <remarks>
    /// External sites can embed the chat using:
    /// <code>&lt;iframe src="/Embed/Chat?token=JWT_HERE"&gt;&lt;/iframe&gt;</code>
    /// The <c>X-Frame-Options</c> header is removed for this route so that browsers
    /// permit cross-origin iframe embedding.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> Chat([FromQuery] string? token)
    {
        // Remove X-Frame-Options for embed routes so browsers allow iframe embedding.
        Response.Headers.Remove("X-Frame-Options");
        Response.Headers.Append("Content-Security-Policy", "frame-ancestors *");

        if (string.IsNullOrWhiteSpace(token))
        {
            return Content(RenderEmbedError("No access token provided. Please supply ?token=JWT_HERE."), "text/html");
        }

        var principal = _jwtService.ValidateToken(token);
        if (principal is null)
        {
            return Content(RenderEmbedError("Invalid or expired token. Please request a new embed token."), "text/html");
        }

        var models = await _aiChatService.GetAvailableModelsAsync();
        ViewBag.Token = token;
        ViewBag.AvailableModels = models.ToList();
        return View();
    }

    /// <summary>
    /// Validates a JWT token and returns session metadata that the embedding page can
    /// use to initialise the embedded chat.
    /// </summary>
    /// <param name="token">The JWT token to validate.</param>
    /// <returns>
    /// A JSON object with <c>valid: true</c> and user information when the token is
    /// accepted; <c>valid: false</c> with an error description when it is rejected.
    /// </returns>
    [HttpGet]
    public IActionResult Token([FromQuery] string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Json(new { valid = false, error = "Token is required." });

        var principal = _jwtService.ValidateToken(token);
        if (principal is null)
            return Json(new { valid = false, error = "Invalid or expired token." });

        var userId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var email = principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

        return Json(new { valid = true, userId, email });
    }

    /// <summary>
    /// Accepts a chat message from the embedded chat UI and returns the AI response.
    /// The request must include a valid JWT token in the <c>X-Embed-Token</c> header
    /// or as a <c>token</c> query-string parameter.
    /// </summary>
    /// <param name="request">The message payload including the user message and selected model.</param>
    /// <returns>
    /// JSON containing <c>content</c> (the AI reply) on success, or an error object
    /// when the token is invalid or the AI service fails.
    /// </returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Send([FromBody] EmbedSendRequest request)
    {
        // Validate token from header or query string
        var token = Request.Headers["X-Embed-Token"].FirstOrDefault()
                    ?? Request.Query["token"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(token) || _jwtService.ValidateToken(token) is null)
            return Unauthorized(new { error = "Invalid or missing embed token." });

        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { error = "Message is required." });

        var chatRequest = new ChatRequest(
            Model: request.Model ?? "gpt-3.5-turbo",
            SystemPrompt: "You are a helpful AI assistant embedded in a partner website.",
            Messages: new List<(string Role, string Content)> { ("user", request.Message) }
        );

        var response = await _aiChatService.SendMessageAsync(chatRequest);
        if (!response.Success)
            return BadRequest(new { error = response.Error });

        return Json(new { content = response.Content, tokensUsed = response.TokensUsed });
    }

    /// <summary>
    /// Renders a read-only public dashboard view for external embedding.
    /// </summary>
    [HttpGet("/embed/dashboard/{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> Dashboard(string slug, [FromServices] IDashboardService dashboardService)
    {
        Response.Headers.Remove("X-Frame-Options");
        Response.Headers.Append("Content-Security-Policy", "frame-ancestors *");

        if (string.IsNullOrWhiteSpace(slug))
            return Content("Invalid embed link.", "text/plain");

        var dashboard = await dashboardService.GetBySlugAsync(slug);
        if (dashboard == null)
            return View("DashboardRevoked");

        return View("~/Views/Embed/Dashboard.cshtml", dashboard);
    }

    /// <summary>
    /// Builds a self-contained HTML error page suitable for display inside an iframe
    /// when token validation fails.
    /// </summary>
    /// <param name="message">The human-readable error message to display.</param>
    /// <returns>An HTML string with a styled error card.</returns>
    private static string RenderEmbedError(string message) => $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""utf-8""/>
    <meta name=""viewport"" content=""width=device-width,initial-scale=1""/>
    <title>ChatPortal Embed Error</title>
    <style>
        body{{margin:0;font-family:system-ui,sans-serif;display:flex;align-items:center;
              justify-content:center;height:100vh;background:#0f1117;color:#e2e8f0;}}
        .card{{text-align:center;padding:2rem;max-width:360px;}}
        .icon{{font-size:3rem;margin-bottom:1rem;opacity:0.5;}}
        h3{{margin:0 0 .5rem;font-size:1.1rem;}}
        p{{color:#94a3b8;font-size:.875rem;margin:0;}}
    </style>
</head>
<body>
    <div class=""card"">
        <div class=""icon"">🔒</div>
        <h3>Access Denied</h3>
        <p>{System.Net.WebUtility.HtmlEncode(message)}</p>
    </div>
</body>
</html>";
}

/// <summary>
/// Request model for the <see cref="EmbedController.Send"/> endpoint.
/// </summary>
public class EmbedSendRequest
{
    /// <summary>Gets or sets the user's message text.</summary>
    public string? Message { get; set; }

    /// <summary>Gets or sets the AI model identifier to use for the response.</summary>
    public string? Model { get; set; }

    /// <summary>Gets or sets the embed JWT token (may also be supplied via header).</summary>
    public string? Token { get; set; }
}
