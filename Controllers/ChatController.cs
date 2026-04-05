using ChatPortal.Services;
using ChatPortal.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatPortal.Controllers;

/// <summary>
/// Handles the main chat interface, including rendering the chat page and
/// accepting AI message requests from the browser.
/// </summary>
public class ChatController : Controller
{
    private readonly IAIChatService _aiChatService;
    private readonly IDataConnectionService _dataConnection;
    private readonly IDataChatService _dataChatService;
    private readonly ILogger<ChatController> _logger;

    /// <summary>
    /// Initialises a new instance of <see cref="ChatController"/>.
    /// </summary>
    /// <param name="aiChatService">Service used to query available AI models and send messages.</param>
    /// <param name="dataConnection">Service used to load user data sources.</param>
    /// <param name="dataChatService">Service used to query data sources with AI.</param>
    /// <param name="logger">Logger for recording errors.</param>
    public ChatController(IAIChatService aiChatService, IDataConnectionService dataConnection,
        IDataChatService dataChatService, ILogger<ChatController> logger)
    {
        _aiChatService = aiChatService;
        _dataConnection = dataConnection;
        _dataChatService = dataChatService;
        _logger = logger;
    }

    private int? GetUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    /// <summary>
    /// Renders the main chat page, populating the view model with the list of
    /// available AI models retrieved from the AI service.
    /// </summary>
    /// <returns>The Chat/Index Razor view pre-populated with <see cref="ChatViewModel"/>.</returns>
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

        var vm = new ChatViewModel
        {
            AvailableModels = models,
            DataSources = dataSources,
            Sessions = new List<ChatSessionViewModel>
            {
                new() { Id = 1, Title = "Getting Started", CreatedAt = DateTime.UtcNow.AddDays(-1) },
                new() { Id = 2, Title = "Code Review Help", CreatedAt = DateTime.UtcNow.AddHours(-3) }
            }
        };
        return View(vm);
    }

    /// <summary>
    /// Accepts a chat message from the browser, forwards it to the AI service, and
    /// returns the AI-generated response as JSON.
    /// </summary>
    /// <param name="request">
    /// The message payload containing the user message text, the selected AI model,
    /// and an optional session identifier.
    /// </param>
    /// <returns>
    /// A <see cref="JsonResult"/> with <c>content</c> (AI reply text) and
    /// <c>tokensUsed</c> on success; a <c>400 Bad Request</c> with an <c>error</c>
    /// property on failure.
    /// </returns>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Send([FromBody] SendMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { error = "Message is required." });

        try
        {
            var userId = GetUserId();

            if (request.DataSourceId.HasValue && userId.HasValue)
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

            var chatRequest = new ChatRequest(
                Model: request.Model ?? "command-a-03-2025",
                SystemPrompt: "You are a helpful AI assistant.",
                Messages: new List<AIChatMessage> { new AIChatMessage("user", request.Message) }
            );

            var aiResponse = await _aiChatService.SendMessageAsync(chatRequest);
            if (!aiResponse.Success)
                return BadRequest(new { error = aiResponse.Error ?? "The AI service was unable to process your request." });

            return Json(new { content = aiResponse.Content, tokensUsed = aiResponse.TokensUsed });
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
    /// <summary>Gets or sets the user's message text.</summary>
    public string? Message { get; set; }

    /// <summary>Gets or sets the AI model identifier (e.g. <c>gpt-4</c>).</summary>
    public string? Model { get; set; }

    /// <summary>Gets or sets the optional session ID for conversation tracking.</summary>
    public int? SessionId { get; set; }

    /// <summary>Gets or sets the optional data source ID for AI data queries.</summary>
    public int? DataSourceId { get; set; }
}
