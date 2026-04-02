using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ChatPortal.Models.Entities;
public class Webhook
{
    [Key] public int Id { get; set; }
    public int UserId { get; set; }
    [ForeignKey("UserId")] public virtual User User { get; set; } = null!;
    [Required, MaxLength(512)] public string Url { get; set; } = string.Empty;
    public string? Events { get; set; }
    [MaxLength(256)] public string? Secret { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastTriggeredAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}