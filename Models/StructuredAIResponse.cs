namespace ChatPortal.Models;

public class StructuredAIResponse
{
    public bool Success { get; set; }
    public string? Query { get; set; }
    public object? Result { get; set; }
    public string? Narrative { get; set; }
    public List<string> Prompts { get; set; } = new();
    public List<string> Examples { get; set; } = new();
    public ChartData? ChartData { get; set; }
    public string? Error { get; set; }
    public int CreditsUsed { get; set; }
}

public class ChartData
{
    public string ChartType { get; set; } = "bar";
    public List<string> Labels { get; set; } = new();
    public List<ChartDataset> Datasets { get; set; } = new();
}

public class ChartDataset
{
    public string Label { get; set; } = string.Empty;
    public List<double> Data { get; set; } = new();
    public string? BackgroundColor { get; set; }
    public string? BorderColor { get; set; }
}
