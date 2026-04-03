using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatPortal.Models.Entities;

public class Workspace
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public int OwnerId { get; set; }
    [ForeignKey("OwnerId")]
    public virtual User Owner { get; set; } = null!;

    // Organization relationship - Required
    public int OrganizationId { get; set; }
    [ForeignKey("OrganizationId")]
    public virtual Organization Organization { get; set; } = null!;

    public int? TeamId { get; set; }
    [ForeignKey("TeamId")]
    public virtual Team? Team { get; set; }

    [MaxLength(50)]
    public string ChatAgentContext { get; set; } = "general";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();
    public virtual ICollection<Agent> Agents { get; set; } = new List<Agent>();
}
