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
            var response = await CallAIForStructuredResponseAsync(userQuestion, ds.SourceType, ds.SchemaSnapshot, sampleJson);
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
        string question, string sourceType, string? schema, string sampleData)
    {
        var aiSettings = _configuration.GetSection("AiSettings");
        var apiKey = aiSettings["ApiKey"];

        if (string.IsNullOrEmpty(apiKey) || apiKey == "your-openai-api-key-here")
        {
            return BuildDemoResponse(question, sourceType);
        }

        var systemPrompt = BuildSystemPrompt(sourceType, schema);
        var userPrompt = $"Question: {question}\n\nSample data (first 20 rows):\n{sampleData}\n\n" +
                         "Respond ONLY with a valid JSON object matching the StructuredAIResponse schema.";

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var payload = new
        {
            model = aiSettings["DefaultModel"] ?? "gpt-3.5-turbo",
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            max_tokens = 2048,
            temperature = 0.3
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var httpResponse = await _httpClient.PostAsync($"{aiSettings["BaseUrl"]}/v1/chat/completions", content);

        if (!httpResponse.IsSuccessStatusCode)
            return BuildDemoResponse(question, sourceType);

        var responseJson = await httpResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseJson);
        var text = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "{}";

        try
        {
            var structured = JsonSerializer.Deserialize<StructuredAIResponse>(text,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return structured ?? BuildDemoResponse(question, sourceType);
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

    private static string BuildSystemPrompt(string sourceType, string? schema)
    {
        var queryLang = sourceType switch
        {
            "SqlServer" => "T-SQL",
            "Oracle" => "PL/SQL",
            _ => "natural language / LINQ"
        };

        return $$"""
You are an expert data analyst. The user has a {{sourceType}} data source.
Schema: {{schema ?? "unknown"}}

Respond ONLY with a JSON object in this exact format:
{
  "success": true,
  "query": "the {{queryLang}} query or expression used",
  "result": [/* array of result rows */],
  "narrative": "human-readable explanation of the result",
  "prompts": ["follow-up question 1", "follow-up question 2", "follow-up question 3"],
  "examples": ["example query 1", "example query 2", "example query 3"],
  "chartData": {
    "chartType": "bar",
    "labels": ["label1","label2"],
    "datasets": [{ "label": "Value", "data": [1,2], "backgroundColor": "rgba(173,216,230,0.7)" }]
  }
}
Return chartData only when there are numeric results suitable for charting. Use pastel colors.
""";
    }

    private StructuredAIResponse BuildDemoResponse(string question, string sourceType)
    {
        var queryLang = sourceType switch
        {
            "SqlServer" => "SELECT TOP 10 * FROM data_table WHERE ...",
            "Oracle" => "SELECT * FROM data_table WHERE ROWNUM <= 10 ...",
            _ => "data.Where(x => ...).Take(10)"
        };

        return new StructuredAIResponse
        {
            Success = true,
            Query = queryLang,
            Narrative = $"Demo mode: This is a simulated response for your question \"{question}\". Configure your OpenAI API key to get real AI-powered insights from your data.",
            Result = new List<Dictionary<string, object>>
            {
                new() { ["Column1"] = "Sample A", ["Value"] = 42 },
                new() { ["Column1"] = "Sample B", ["Value"] = 78 },
                new() { ["Column1"] = "Sample C", ["Value"] = 35 }
            },
            Prompts = new List<string>
            {
                "Show the top 10 records",
                "What is the average value?",
                "Group by category and count"
            },
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
