using ChatPortal.Data;
using ChatPortal.Models.Entities;
using ChatPortal.Services;
using ChatPortal.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ChatPortal.Controllers;

/// <summary>
/// Handles the main chat interface, including rendering the chat page and
/// accepting AI message requests from the browser.
/// </summary>
[Authorize]
public class ChatController : Controller
{
    private readonly IAIChatService _aiChatService;
    private readonly IDataConnectionService _dataConnection;
    private readonly IDataChatService _dataChatService;
    private readonly AppDbContext _context;
    private readonly ILogger<ChatController> _logger;

    /// <summary>
    /// Initialises a new instance of <see cref="ChatController"/>.
    /// </summary>
    public ChatController(IAIChatService aiChatService, IDataConnectionService dataConnection,
        IDataChatService dataChatService, AppDbContext context, ILogger<ChatController> logger)
    {
        _aiChatService = aiChatService;
        _dataConnection = dataConnection;
        _dataChatService = dataChatService;
        _context = context;
        _logger = logger;
    }

    private int? GetUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    /// <summary>
    /// Renders the main chat page, populating the view model with the list of
    /// available AI models retrieved from the AI service.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        List<string> models;
        try
        {
            models = (await _aiChatService.GetAvailableModelsAsync()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve available AI models");
            models = new List<string> { "gpt-3.5-turbo" };
            TempData["Error"] = "Could not load AI model list. A default model has been selected.";
        }

        var dataSources = new List<UserDataSourceViewModel>();
        var userId = GetUserId();
        if (userId.HasValue)
        {
            try
            {
                var sources = await _dataConnection.GetUserDataSourcesAsync(userId.Value);
                dataSources = sources.Select(s => new UserDataSourceViewModel
                {
                    Id = s.Id,
                    Name = s.Name,
                    SourceType = s.SourceType,
                    Status = s.Status,
                    CreatedAt = s.CreatedAt
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load user data sources for chat sidebar");
            }
        }

        // Load real per-user chat sessions — no cross-user leakage
        var sessions = new List<ChatSessionViewModel>();
        if (userId.HasValue)
        {
            try
            {
                sessions = await _context.ChatSessions
                    .Where(s => s.UserId == userId.Value && !s.IsArchived)
                    .OrderByDescending(s => s.UpdatedAt)
                    .Select(s => new ChatSessionViewModel
                    {
                        Id = s.Id,
                        Title = s.Title,
                        CreatedAt = s.CreatedAt
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load chat sessions for user {UserId}", userId.Value);
            }
        }

        var vm = new ChatViewModel
        {
            AvailableModels = models,
            DataSources = dataSources,
            Sessions = sessions
        };
        return View(vm);
    }

    /// <summary>
    /// Returns only the current user's chat sessions as JSON.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSessions()
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var sessions = await _context.ChatSessions
            .Where(s => s.UserId == userId.Value && !s.IsArchived)
            .OrderByDescending(s => s.UpdatedAt)
            .Select(s => new { s.Id, s.Title, s.CreatedAt })
            .ToListAsync();

        return Json(new { success = true, sessions });
    }

    /// <summary>
    /// Creates a new chat session bound to the authenticated user and a required datasource.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest request)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
            return Unauthorized();

        if (request.DataSourceId <= 0)
            return BadRequest(new { error = "A datasource with AI Insights is required to start a chat." });

        // Verify the datasource belongs to this user
        var dsExists = await _context.DataSourceConnections
            .AnyAsync(d => d.Id == request.DataSourceId && d.UserId == userId.Value);
        if (!dsExists)
            return BadRequest(new { error = "Data source not found or access denied." });

        var session = new ChatSession
        {
            UserId = userId.Value,
            Title = string.IsNullOrWhiteSpace(request.Title) ? "New Chat" : request.Title,
            DataSourceConnectionId = request.DataSourceId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ChatSessions.Add(session);
        await _context.SaveChangesAsync();

        return Json(new { success = true, sessionId = session.Id });
    }

    /// <summary>
    /// Accepts a chat message, requires a datasource — no plain-AI fallback.
    /// </summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Send([FromBody] SendMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { error = "Message is required." });

        var userId = GetUserId();

        if (!request.DataSourceId.HasValue || !userId.HasValue)
            return BadRequest(new { error = "A datasource with AI Insights is required to start a chat." });

        try
        {
            var response = await _dataChatService.QueryDataSourceAsync(userId.Value, request.DataSourceId.Value, request.Message);
            if (!response.Success)
                return BadRequest(new { error = response.Error });

            return Json(new
            {
                success = true,
                queryDescription = response.QueryDescription,
                query = response.Query,
                prompts = response.Prompts,
                creditsUsed = 5
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while sending chat message");
            return StatusCode(500, new { error = "An unexpected error occurred. Please try again." });
        }
    }
}

/// <summary>
/// Request body model used by the <see cref="ChatController.Send"/> endpoint.
/// </summary>
public class SendMessageRequest
{
    public string? Message { get; set; }
    public string? Model { get; set; }
    public int? SessionId { get; set; }
    public int? DataSourceId { get; set; }
}

/// <summary>
/// Request body model used by the <see cref="ChatController.CreateSession"/> endpoint.
/// </summary>
public class CreateSessionRequest
{
    public int DataSourceId { get; set; }
    public string? Title { get; set; }
}
