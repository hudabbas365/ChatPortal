using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ChatPortal.Models.Entities;

public enum AnnouncementPriority { Informational, Warning, Urgent }

public class Announcement
{
    [Key] public int Id { get; set; }
    [Required, MaxLength(256)] public string Title { get; set; } = string.Empty;
    [Required] public string Content { get; set; } = string.Empty;
    public AnnouncementPriority Priority { get; set; } = AnnouncementPriority.Informational;
    public int? CreatedByAdminId { get; set; }
    [ForeignKey("CreatedByAdminId")] public virtual User? CreatedByAdmin { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public virtual ICollection<AnnouncementReadStatus> ReadStatuses { get; set; } = new List<AnnouncementReadStatus>();
}
