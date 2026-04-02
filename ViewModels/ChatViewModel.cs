namespace ChatPortal.ViewModels;

/// <summary>
/// Represents a single chat message exchanged between a user and the AI assistant.
/// </summary>
public class ChatMessageViewModel
{
    /// <summary>Gets or sets the role of the message author. Typically "User" or "Assistant".</summary>
    public string Role { get; set; } = "User";

    /// <summary>Gets or sets the text content of the message.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC timestamp when the message was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a single chat session, grouping a sequence of messages under a
/// descriptive title and creation timestamp.
/// </summary>
public class ChatSessionViewModel
{
    /// <summary>Gets or sets the numeric identifier for the session (server-side).</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the human-readable session title.</summary>
    public string Title { get; set; } = "New Chat";

    /// <summary>Gets or sets the UTC date and time when the session was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the ordered list of messages in this session.</summary>
    public List<ChatMessageViewModel> Messages { get; set; } = new();
}

/// <summary>
/// View model for the main Chat page, providing the list of sessions, the
/// currently active session, and the AI models available for selection.
/// </summary>
public class ChatViewModel
{
    /// <summary>Gets or sets the list of recent chat sessions shown in the sidebar.</summary>
    public List<ChatSessionViewModel> Sessions { get; set; } = new();

    /// <summary>Gets or sets the session currently displayed in the central chat area.</summary>
    public ChatSessionViewModel? CurrentSession { get; set; }

    /// <summary>Gets or sets the AI model identifiers available for the model-selector dropdown.</summary>
    public List<string> AvailableModels { get; set; } = new();

    /// <summary>Gets or sets the model identifier selected by default.</summary>
    public string SelectedModel { get; set; } = "gpt-3.5-turbo";
}
