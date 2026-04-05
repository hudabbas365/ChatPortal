using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ChatPortal.Models;

namespace ChatPortal.Services;

public interface IDataChatService
{
    Task<StructuredAIResponse> QueryDataSourceAsync(int userId, int dataSourceId, string userQuestion);
}

public class DataChatService : IDataChatService
{
    private readonly IDataConnectionService _dataConnection;
    private readonly ICreditService _creditService;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DataChatService> _logger;

    private static readonly string[] PastelColors =
    {
        "rgba(173, 216, 230, 0.7)",  // light blue
        "rgba(216, 191, 216, 0.7)",  // lavender
        "rgba(144, 238, 144, 0.7)",  // light green
        "rgba(255, 218, 185, 0.7)",  // peach
        "rgba(255, 255, 153, 0.7)",  // yellow
        "rgba(176, 224, 230, 0.7)"   // powder blue
    };

    public DataChatService(IDataConnectionService dataConnection, ICreditService creditService,
        HttpClient httpClient, IConfiguration configuration, ILogger<DataChatService> logger)
    {
        _dataConnection = dataConnection;
        _creditService = creditService;
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<StructuredAIResponse> QueryDataSourceAsync(int userId, int dataSourceId, string userQuestion)
    {
        const int creditCost = 5;

        // Ownership check happens inside GetDataSourceAsync
        var ds = await _dataConnection.GetDataSourceAsync(dataSourceId, userId);
        if (ds == null)
            return new StructuredAIResponse { Success = false, Error = "Data source not found or access denied." };

        // Credit check
        var canDeduct = await _creditService.DeductCreditsAsync(userId, creditCost,
            $"AI query on data source '{ds.Name}'", dataSourceId);
        if (!canDeduct)
            return new StructuredAIResponse { Success = false, Error = "Insufficient credits. Please purchase more credits to continue." };

        try
        {
            // Fetch a limited sample of the data for AI context — never execute the user's raw question as SQL
            List<Dictionary<string, object?>> sampleData;
            try
            {
                var safePreviewQuery = ds.SourceType == "SqlServer"
                    ? "SELECT TOP 20 * FROM (SELECT * FROM INFORMATION_SCHEMA.TABLES) t"
                    : "preview";
                sampleData = await _dataConnection.QueryDataSourceAsync(dataSourceId, userId, safePreviewQuery);
            }
            catch
            {
                sampleData = new List<Dictionary<string, object?>>();
            }

            var sampleJson = JsonSerializer.Serialize(sampleData.Take(20));
            var response = await CallAIForStructuredResponseAsync(userQuestion, ds.SourceType, ds.SchemaSnapshot, sampleJson, null, ds.Name);
            response.CreditsUsed = creditCost;
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying data source {DataSourceId}", dataSourceId);
            return new StructuredAIResponse
            {
                Success = false,
                Error = "An error occurred while processing your query.",
                CreditsUsed = creditCost
            };
        }
    }

    private async Task<StructuredAIResponse> CallAIForStructuredResponseAsync(
        string question, string sourceType, string? schema, string sampleData,
        string? agentName = null, string? dataSourceName = null)
    {
        var aiSettings = _configuration.GetSection("AiSettings");
        var apiKey = aiSettings["ApiKey"];

        if (string.IsNullOrEmpty(apiKey) || apiKey == "your-cohere-api-key-here")
        {
            return BuildDemoResponse(question, sourceType);
        }

        var systemPrompt = BuildSystemPrompt(sourceType, schema, agentName, dataSourceName);
        var userPrompt = $"Question: {question}\n\nSample data (first 20 rows):\n{sampleData}\n\n" +
                         "Respond ONLY with valid JSON in this exact format:\n" +
                         "{ \"QueryDescription\": \"...\", \"Query\": \"...\", \"Prompts\": \"prompt1$prompt2$prompt3\" }";

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        var payload = new Dictionary<string, object>
        {
            ["model"] = "command-a-03-2025",
            ["stream"] = true,
            ["messages"] = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            }
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var baseUrl = aiSettings["BaseUrl"] ?? "https://api.cohere.com";
        var httpResponse = await _httpClient.PostAsync($"{baseUrl}/v2/chat", content);

        if (!httpResponse.IsSuccessStatusCode)
            return BuildDemoResponse(question, sourceType);

        using var stream = await httpResponse.Content.ReadAsStreamAsync();
        using var reader = new System.IO.StreamReader(stream);
        var fullResponse = new StringBuilder();

        while (!reader.EndOfStream)
        {
            string? line = await reader.ReadLineAsync();
            if (string.IsNullOrEmpty(line) || !line.StartsWith("data: ")) continue;
            string data = line[6..];
            if (data == "[DONE]") break;
            try
            {
                using JsonDocument eventDoc = JsonDocument.Parse(data);
                if (eventDoc.RootElement.TryGetProperty("type", out JsonElement typeElement) &&
                    typeElement.GetString() == "content-delta")
                {
                    if (eventDoc.RootElement.TryGetProperty("delta", out JsonElement deltaElement) &&
                        deltaElement.TryGetProperty("message", out JsonElement messageElement) &&
                        messageElement.TryGetProperty("content", out JsonElement contentElement) &&
                        contentElement.TryGetProperty("text", out JsonElement textElement))
                    {
                        fullResponse.Append(textElement.GetString());
                    }
                }
            }
            catch (JsonException) { _logger.LogDebug("Skipped malformed JSON in streaming response"); }
        }

        var text = fullResponse.ToString();

        try
        {
            // Strip markdown code fences if present
            var jsonText = text.Trim();
            if (jsonText.StartsWith("```"))
            {
                var start = jsonText.IndexOf('{');
                var end = jsonText.LastIndexOf('}');
                if (start >= 0 && end > start)
                    jsonText = jsonText[start..(end + 1)];
            }
            else
            {
                jsonText = jsonText.TrimEnd('`').Trim();
            }

            using var parsed = JsonDocument.Parse(jsonText);
            var root = parsed.RootElement;

            var queryDescription = root.TryGetProperty("QueryDescription", out var qd) ? qd.GetString() : null;
            var query = root.TryGetProperty("Query", out var q) ? q.GetString() : null;

            var suggestions = new List<string>();
            if (root.TryGetProperty("Prompts", out var promptsEl) && promptsEl.ValueKind == JsonValueKind.String)
            {
                var promptsStr = promptsEl.GetString() ?? "";
                suggestions = promptsStr.Split('$', StringSplitOptions.RemoveEmptyEntries)
                                        .Select(p => p.Trim())
                                        .Where(p => !string.IsNullOrEmpty(p))
                                        .ToList();
            }

            return new StructuredAIResponse
            {
                Success = true,
                QueryDescription = queryDescription,
                Query = query,
                Narrative = queryDescription,
                Suggestions = suggestions,
                Prompts = suggestions,
                Examples = GetExamples(sourceType)
            };
        }
        catch
        {
            return new StructuredAIResponse
            {
                Success = true,
                Narrative = text,
                Prompts = new List<string> { "Show more details", "Create a chart", "Export results" },
                Examples = GetExamples(sourceType)
            };
        }
    }

    private static string BuildSystemPrompt(string sourceType, string? schema,
        string? agentName = null, string? dataSourceName = null)
    {
        if (!string.IsNullOrEmpty(agentName) && !string.IsNullOrEmpty(dataSourceName))
        {
            return $"You are an AI Agent for {agentName}.\n" +
                   $"Generate queries for {dataSourceName} using the following tables/views:\n" +
                   $"{schema ?? "unknown"}.\n" +
                   "Maintain relationships between tables if they exist.";
        }

        var queryLang = sourceType switch
        {
            "SqlServer" => "T-SQL",
            "Oracle" => "PL/SQL",
            _ => "natural language / LINQ"
        };

        return $"You are an expert data analyst. The user has a {sourceType} data source.\n" +
               $"Generate {queryLang} queries using the following schema:\n" +
               $"{schema ?? "unknown"}.\n" +
               "Maintain relationships between tables if they exist." +
               "Dont Invent a new Column the return will be as per the existing schema." +
               "return values \"QueryDescription\", \"Query\", \"Prompts\" " +
               "Rules:\n" +
               "3 Promopts will be return split by $" +
               "Any special charcter or Welcome message will be consider randon result from schema";
    }

    private StructuredAIResponse BuildDemoResponse(string question, string sourceType)
    {
        var queryLang = sourceType switch
        {
            "SqlServer" => "SELECT TOP 10 * FROM data_table WHERE ...",
            "Oracle" => "SELECT * FROM data_table WHERE ROWNUM <= 10 ...",
            _ => "data.Where(x => ...).Take(10)"
        };

        var suggestions = new List<string>
        {
            "Show the top 10 records",
            "What is the average value?",
            "Group by category and count"
        };

        return new StructuredAIResponse
        {
            Success = true,
            Query = queryLang,
            QueryDescription = $"Demo mode: This is a simulated response for your question \"{question}\". Configure your Cohere API key to get real AI-powered insights from your data.",
            Narrative = $"Demo mode: This is a simulated response for your question \"{question}\". Configure your Cohere API key to get real AI-powered insights from your data.",
            Result = new List<Dictionary<string, object>>
            {
                new() { ["Column1"] = "Sample A", ["Value"] = 42 },
                new() { ["Column1"] = "Sample B", ["Value"] = 78 },
                new() { ["Column1"] = "Sample C", ["Value"] = 35 }
            },
            Suggestions = suggestions,
            Prompts = suggestions,
            Examples = GetExamples(sourceType),
            ChartData = new ChartData
            {
                ChartType = "bar",
                Labels = new List<string> { "Sample A", "Sample B", "Sample C" },
                Datasets = new List<ChartDataset>
                {
                    new()
                    {
                        Label = "Value",
                        Data = new List<double> { 42, 78, 35 },
                        BackgroundColor = PastelColors[0],
                        BorderColor = "rgba(100,149,237,0.9)"
                    }
                }
            }
        };
    }

    private static List<string> GetExamples(string sourceType) => sourceType switch
    {
        "SqlServer" => new List<string>
        {
            "Show me the top 10 customers by revenue",
            "What are the monthly sales trends?",
            "Compare product performance across regions"
        },
        "Oracle" => new List<string>
        {
            "List employees by department",
            "Show average salary per department",
            "Find top 5 highest-paid employees"
        },
        _ => new List<string>
        {
            "What are the column names in this dataset?",
            "Show me the first 10 rows",
            "What is the total count of records?"
        }
    };
}
