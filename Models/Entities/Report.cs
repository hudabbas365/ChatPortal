using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ChatPortal.Models.Entities;
public class Report
{
    [Key] public int Id { get; set; }
    public int UserId { get; set; }
    [ForeignKey("UserId")] public virtual User User { get; set; } = null!;
    [Required, MaxLength(256)] public string Title { get; set; } = string.Empty;
    [MaxLength(50)] public string? Type { get; set; }
    public string? Parameters { get; set; }
    [MaxLength(512)] public string? GeneratedFileUrl { get; set; }
    [Required, MaxLength(50)] public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ScheduledAt { get; set; }
}