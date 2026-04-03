using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatPortal.Models.Entities;

public class Organization
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string? Industry { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(256)]
    public string? Website { get; set; }

    // Owner/Creator of the organization
    public int OwnerId { get; set; }
    [ForeignKey("OwnerId")]
    public virtual User Owner { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    /// <summary>Unique business identifier (GUID) — additive, int Id remains the PK for EF.</summary>
    public Guid UniqueId { get; set; } = Guid.NewGuid();

    /// <summary>Tracks the last time any activity occurred in this organization (used for 30-day retention policy).</summary>
    public DateTime? LastActivityAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<OrganizationMember> Members { get; set; } = new List<OrganizationMember>();
    public virtual ICollection<Workspace> Workspaces { get; set; } = new List<Workspace>();
    public virtual ICollection<Team> Teams { get; set; } = new List<Team>();
}
