using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatPortal.Models.Entities;

public class Workspace
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public Guid OwnerId { get; set; }
    [ForeignKey("OwnerId")]
    public virtual User Owner { get; set; } = null!;

    // Organization relationship - Required
    public Guid OrganizationId { get; set; }
    [ForeignKey("OrganizationId")]
    public virtual Organization Organization { get; set; } = null!;

    public Guid? TeamId { get; set; }
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
