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

            if (string.IsNullOrEmpty(apiKey) || apiKey == "your-openai-api-key-here")
            {
                // Return stub response for demo
                await Task.Delay(500);
                return new ChatResponse(true, $"This is a demo response to: \"{request.Messages.LastOrDefault().Content}\". Configure your OpenAI API key to get real AI responses.", 42, null);
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var messages = new List<object>();
            if (!string.IsNullOrEmpty(request.SystemPrompt))
                messages.Add(new { role = "system", content = request.SystemPrompt });

            foreach (var msg in request.Messages)
                messages.Add(new { role = msg.Role.ToLower(), content = msg.Content });

            var payload = new
            {
                model = request.Model ?? "gpt-3.5-turbo",
                messages,
                max_tokens = 1024
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{aiSettings["BaseUrl"]}/v1/chat/completions", content);

            if (!response.IsSuccessStatusCode)
                return new ChatResponse(false, null, 0, $"API error: {response.StatusCode}");

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            var text = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "";

            var tokens = doc.RootElement.GetProperty("usage").GetProperty("total_tokens").GetInt32();
            return new ChatResponse(true, text, tokens, null);
        }
        catch (Exception ex)
        {
            return new ChatResponse(false, null, 0, ex.Message);
        }
    }

    public Task<IEnumerable<string>> GetAvailableModelsAsync()
    {
        return Task.FromResult<IEnumerable<string>>(new[] { "gpt-3.5-turbo", "gpt-4" });
    }
}
