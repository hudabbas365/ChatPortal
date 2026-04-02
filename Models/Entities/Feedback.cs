using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ChatPortal.Models.Entities;
public class Feedback
{
    [Key] public int Id { get; set; }
    public int? UserId { get; set; }
    [ForeignKey("UserId")] public virtual User? User { get; set; }
    [MaxLength(100)] public string? Name { get; set; }
    [MaxLength(256)] public string? Email { get; set; }
    [MaxLength(256)] public string? Subject { get; set; }
    [Required] public string Message { get; set; } = string.Empty;
    public int? Rating { get; set; }
    [MaxLength(50)] public string Status { get; set; } = "New";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}