using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatPortal.Models.Entities;

public class TeamWorkspacePermission
{
    [Key]
    public int Id { get; set; }

    public int TeamId { get; set; }
    [ForeignKey("TeamId")]
    public virtual Team Team { get; set; } = null!;

    public int WorkspaceId { get; set; }
    [ForeignKey("WorkspaceId")]
    public virtual Workspace Workspace { get; set; } = null!;

    [MaxLength(50)]
    public string Permission { get; set; } = "View"; // View, Edit, Manage

    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    public int GrantedBy { get; set; }
    [ForeignKey("GrantedBy")]
    public virtual User Granter { get; set; } = null!;
}
