using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ChatPortal.Models.Entities;
public class Recommendation
{
    [Key] public int Id { get; set; }
    public int UserId { get; set; }
    [ForeignKey("UserId")] public virtual User User { get; set; } = null!;
    [MaxLength(100)] public string? Type { get; set; }
    [MaxLength(256)] public string? Title { get; set; }
    [MaxLength(1000)] public string? Description { get; set; }
    public double Score { get; set; }
    public string? Metadata { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}