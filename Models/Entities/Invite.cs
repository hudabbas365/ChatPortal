using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ChatPortal.Models.Entities;
public class Invite
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TeamId { get; set; }
    [ForeignKey("TeamId")] public virtual Team Team { get; set; } = null!;
    [Required, MaxLength(256)] public string Email { get; set; } = string.Empty;
    public Guid InvitedById { get; set; }
    [ForeignKey("InvitedById")] public virtual User InvitedBy { get; set; } = null!;
    [Required] public string Token { get; set; } = Guid.NewGuid().ToString();
    [Required, MaxLength(50)] public string Status { get; set; } = "Pending";
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}