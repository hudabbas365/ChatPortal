using ChatPortal.Services;
using ChatPortal.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ChatPortal.Controllers;

public class ChatController : Controller
{
    private readonly IAIChatService _aiChatService;

    public ChatController(IAIChatService aiChatService)
    {
        _aiChatService = aiChatService;
    }

    public async Task<IActionResult> Index()
    {
        var models = await _aiChatService.GetAvailableModelsAsync();
        var vm = new ChatViewModel
        {
            AvailableModels = models.ToList(),
            Sessions = new List<ChatSessionViewModel>
            {
                new() { Id = 1, Title = "Getting Started", CreatedAt = DateTime.UtcNow.AddDays(-1) },
                new() { Id = 2, Title = "Code Review Help", CreatedAt = DateTime.UtcNow.AddHours(-3) }
            }
        };
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Send([FromBody] SendMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { error = "Message is required." });

        var chatRequest = new ChatRequest(
            Model: request.Model ?? "gpt-3.5-turbo",
            SystemPrompt: "You are a helpful AI assistant.",
            Messages: new List<(string Role, string Content)> { ("user", request.Message) }
        );

        var response = await _aiChatService.SendMessageAsync(chatRequest);
        if (!response.Success)
            return BadRequest(new { error = response.Error });

        return Json(new { content = response.Content, tokensUsed = response.TokensUsed });
    }
}

public class SendMessageRequest
{
    public string? Message { get; set; }
    public string? Model { get; set; }
    public int? SessionId { get; set; }
}
