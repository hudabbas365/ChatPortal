using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ChatPortal.Models.Entities;
public class TeamMember
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TeamId { get; set; }
    [ForeignKey("TeamId")] public virtual Team Team { get; set; } = null!;
    public Guid UserId { get; set; }
    [ForeignKey("UserId")] public virtual User User { get; set; } = null!;
    [MaxLength(50)] public string Role { get; set; } = "Member";
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}