namespace ChatPortal.ViewModels;

public class AgentChatViewModel
{
    public Guid AgentId { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public string? AgentDescription { get; set; }
    public string AgentType { get; set; } = "general";
    public string? ModelName { get; set; }
    public Guid? SessionId { get; set; }
    public List<ChatMessageDto> History { get; set; } = new();
}

public record ChatMessageDto(string Role, string Content, DateTime CreatedAt);
