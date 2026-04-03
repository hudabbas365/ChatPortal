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

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<OrganizationMember> Members { get; set; } = new List<OrganizationMember>();
    public virtual ICollection<Workspace> Workspaces { get; set; } = new List<Workspace>();
    public virtual ICollection<Team> Teams { get; set; } = new List<Team>();
}
