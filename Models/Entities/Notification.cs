using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ChatPortal.Models.Entities;
public class Notification
{
    [Key] public int Id { get; set; }
    public int UserId { get; set; }
    [ForeignKey("UserId")] public virtual User User { get; set; } = null!;
    [Required, MaxLength(256)] public string Title { get; set; } = string.Empty;
    [Required] public string Content { get; set; } = string.Empty;
    [MaxLength(50)] public string? Type { get; set; }
    public bool IsRead { get; set; } = false;
    [MaxLength(512)] public string? ActionUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>informational | warning | urgent</summary>
    [MaxLength(50)]
    public string Priority { get; set; } = "informational";

    public DateTime? DismissedAt { get; set; }
}