using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatPortal.Models.Entities;

public class ChatSession
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [Required, MaxLength(256)]
    public string Title { get; set; } = "New Chat";

    public int? ModelId { get; set; }

    [ForeignKey("ModelId")]
    public virtual AIModel? Model { get; set; }

    public int? WorkspaceId { get; set; }

    [ForeignKey("WorkspaceId")]
    public virtual Workspace? Workspace { get; set; }

    public int? AgentId { get; set; }

    [ForeignKey("AgentId")]
    public virtual Agent? Agent { get; set; }

    // DataSourceConnection FK — required to enforce AI Insights binding
    public int? DataSourceConnectionId { get; set; }

    [ForeignKey("DataSourceConnectionId")]
    public virtual DataSourceConnection? DataSourceConnection { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsArchived { get; set; } = false;

    public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}