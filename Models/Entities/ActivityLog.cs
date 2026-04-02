using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ChatPortal.Models.Entities;
public class ActivityLog
{
    [Key] public int Id { get; set; }
    public int UserId { get; set; }
    [ForeignKey("UserId")] public virtual User User { get; set; } = null!;
    [Required, MaxLength(256)] public string Action { get; set; } = string.Empty;
    public string? Details { get; set; }
    [MaxLength(50)] public string? IpAddress { get; set; }
    [MaxLength(512)] public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}