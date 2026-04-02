using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ChatPortal.Models.Entities;
public class DataSource
{
    [Key] public int Id { get; set; }
    public int UserId { get; set; }
    [ForeignKey("UserId")] public virtual User User { get; set; } = null!;
    [Required, MaxLength(256)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(50)] public string Type { get; set; } = "API";
    public string? ConnectionDetails { get; set; }
    [Required, MaxLength(50)] public string Status { get; set; } = "Active";
    public DateTime? LastSyncAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}