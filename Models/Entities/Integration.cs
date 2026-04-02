using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ChatPortal.Models.Entities;
public class Integration
{
    [Key] public int Id { get; set; }
    public int UserId { get; set; }
    [ForeignKey("UserId")] public virtual User User { get; set; } = null!;
    [Required, MaxLength(100)] public string ServiceName { get; set; } = string.Empty;
    public string? Config { get; set; }
    [Required, MaxLength(50)] public string Status { get; set; } = "Connected";
    public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
}