using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatPortal.Models.Entities;

public class AIModel
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Provider { get; set; }

    [MaxLength(50)]
    public string? Version { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
    public int MaxTokens { get; set; } = 4096;

    [Column(TypeName = "decimal(18,6)")]
    public decimal CostPerToken { get; set; } = 0.000002m;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();
    public virtual ICollection<TrainingJob> TrainingJobs { get; set; } = new List<TrainingJob>();
}