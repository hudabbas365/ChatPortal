using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatPortal.Models.Entities;

public class OrganizationMember
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrganizationId { get; set; }
    [ForeignKey("OrganizationId")]
    public virtual Organization Organization { get; set; } = null!;

    public Guid UserId { get; set; }
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [Required, MaxLength(50)]
    public string Role { get; set; } = "Member"; // Admin, Member, Viewer

    public bool IsActive { get; set; } = true;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
