using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ChatPortal.Models.Entities;
public class Team
{
    [Key] public int Id { get; set; }
    [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
    public int OwnerId { get; set; }
    [ForeignKey("OwnerId")] public virtual User Owner { get; set; } = null!;

    // Organization relationship
    public int OrganizationId { get; set; }
    [ForeignKey("OrganizationId")] public virtual Organization Organization { get; set; } = null!;

    [MaxLength(500)] public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public virtual ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();
    public virtual ICollection<Invite> Invites { get; set; } = new List<Invite>();
    public virtual ICollection<TeamWorkspacePermission> WorkspacePermissions { get; set; } = new List<TeamWorkspacePermission>();
}
