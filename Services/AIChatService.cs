using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ChatPortal.Services;

public class AIChatService : IAIChatService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public AIChatService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<ChatResponse> SendMessageAsync(ChatRequest request)
    {
        try
        {
            var aiSettings = _configuration.GetSection("AiSettings");
            var apiKey = aiSettings["ApiKey"];

            if (string.IsNullOrEmpty(apiKey) || apiKey == "your-cohere-api-key-here")
            {
                // Return stub response for demo
                await Task.Delay(500);
                var lastMessage = request.Messages.LastOrDefault();
                var userContent = lastMessage != default ? lastMessage.Content : "your message";
                return new ChatResponse(true, $"This is a demo response to: \"{userContent}\". Configure your Cohere API key to get real AI responses.", 42, null);
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var messages = new List<object>();
            if (!string.IsNullOrEmpty(request.SystemPrompt))
                messages.Add(new { role = "system", content = request.SystemPrompt });

            foreach (var msg in request.Messages)
                messages.Add(new { role = msg.Role.ToLower(), content = msg.Content });

            var payload = new
            {
                model = request.Model ?? aiSettings["DefaultModel"] ?? "command-r-plus",
                messages
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var baseUrl = aiSettings["BaseUrl"] ?? "https://api.cohere.com";
            var response = await _httpClient.PostAsync($"{baseUrl}/v2/chat", content);

            if (!response.IsSuccessStatusCode)
                return new ChatResponse(false, null, 0, $"API error: {response.StatusCode}");

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);

            // Cohere v2 response: message.content[0].text
            var text = doc.RootElement
                .GetProperty("message")
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString() ?? "";

            // Approximate token count from usage if available
            var tokens = 0;
            if (doc.RootElement.TryGetProperty("usage", out var usage) &&
                usage.TryGetProperty("tokens", out var tokensEl))
            {
                tokensEl.TryGetProperty("input_tokens", out var inputTokens);
                tokensEl.TryGetProperty("output_tokens", out var outputTokens);
                tokens = (inputTokens.ValueKind == JsonValueKind.Number ? inputTokens.GetInt32() : 0)
                       + (outputTokens.ValueKind == JsonValueKind.Number ? outputTokens.GetInt32() : 0);
            }

            return new ChatResponse(true, text, tokens, null);
        }
        catch (Exception ex)
        {
            return new ChatResponse(false, null, 0, ex.Message);
        }
    }

    public Task<IEnumerable<string>> GetAvailableModelsAsync()
    {
        return Task.FromResult<IEnumerable<string>>(new[] { "command-r-plus", "command-r", "command" });
    }
}
