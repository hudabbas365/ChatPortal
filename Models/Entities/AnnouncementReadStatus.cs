using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ChatPortal.Models.Entities;

public class AnnouncementReadStatus
{
    [Key] public int Id { get; set; }
    public int AnnouncementId { get; set; }
    [ForeignKey("AnnouncementId")] public virtual Announcement Announcement { get; set; } = null!;
    public int UserId { get; set; }
    [ForeignKey("UserId")] public virtual User User { get; set; } = null!;
    public bool IsRead { get; set; } = false;
    public bool IsDismissed { get; set; } = false;
    public DateTime? ReadAt { get; set; }
}
