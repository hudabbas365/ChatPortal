using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ChatPortal.Models.Entities;

public class AnnouncementReadStatus
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AnnouncementId { get; set; }
    [ForeignKey("AnnouncementId")] public virtual Announcement Announcement { get; set; } = null!;
    public Guid UserId { get; set; }
    [ForeignKey("UserId")] public virtual User User { get; set; } = null!;
    public bool IsRead { get; set; } = false;
    public bool IsDismissed { get; set; } = false;
    public DateTime? ReadAt { get; set; }
}
