namespace ChatPortal.Services;

public record ChatRequest(string Model, string SystemPrompt, List<(string Role, string Content)> Messages);
public record ChatResponse(bool Success, string? Content, int TokensUsed, string? Error);

public interface IAIChatService
{
    Task<ChatResponse> SendMessageAsync(ChatRequest request);
    Task<IEnumerable<string>> GetAvailableModelsAsync();
}
