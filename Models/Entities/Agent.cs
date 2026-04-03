using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatPortal.Models.Entities;

public class Agent
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public int WorkspaceId { get; set; }
    [ForeignKey("WorkspaceId")]
    public virtual Workspace Workspace { get; set; } = null!;

    [MaxLength(50)]
    public string AgentType { get; set; } = "general"; // general, code, writing, data, research, customer-service

    [MaxLength(2000)]
    public string? SystemPrompt { get; set; }

    [MaxLength(50)]
    public string? ModelName { get; set; } = "GPT-3.5 Turbo";

    public bool IsActive { get; set; } = true;

    public int CreatedBy { get; set; }
    [ForeignKey("CreatedBy")]
    public virtual User Creator { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Configuration settings
    public decimal? Temperature { get; set; } = 0.7m;
    public int? MaxTokens { get; set; } = 2000;

    // Navigation properties
    public virtual ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();
}
