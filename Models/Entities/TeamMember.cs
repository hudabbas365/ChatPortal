using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ChatPortal.Models.Entities;
public class TeamMember
{
    [Key] public int Id { get; set; }
    public int TeamId { get; set; }
    [ForeignKey("TeamId")] public virtual Team Team { get; set; } = null!;
    public int UserId { get; set; }
    [ForeignKey("UserId")] public virtual User User { get; set; } = null!;
    [MaxLength(50)] public string Role { get; set; } = "Member";
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}