using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatPortal.Models.Entities;

public class Invitation
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, EmailAddress, MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string Token { get; set; } = string.Empty;

    public Guid? OrganizationId { get; set; }
    [ForeignKey("OrganizationId")]
    public virtual Organization? Organization { get; set; }

    public Guid? TeamId { get; set; }
    [ForeignKey("TeamId")]
    public virtual Team? Team { get; set; }

    [MaxLength(50)]
    public string Role { get; set; } = "Member"; // Member, Admin, Viewer

    public Guid InvitedBy { get; set; }
    [ForeignKey("InvitedBy")]
    public virtual User Inviter { get; set; } = null!;

    public bool IsAccepted { get; set; } = false;
    public DateTime? AcceptedAt { get; set; }

    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
