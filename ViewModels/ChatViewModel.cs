namespace ChatPortal.ViewModels;

public class ChatMessageViewModel
{
    public string Role { get; set; } = "User";
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ChatSessionViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = "New Chat";
    public DateTime CreatedAt { get; set; }
    public List<ChatMessageViewModel> Messages { get; set; } = new();
}

public class ChatViewModel
{
    public List<ChatSessionViewModel> Sessions { get; set; } = new();
    public ChatSessionViewModel? CurrentSession { get; set; }
    public List<string> AvailableModels { get; set; } = new();
    public string SelectedModel { get; set; } = "gpt-3.5-turbo";
}
