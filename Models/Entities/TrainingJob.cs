using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ChatPortal.Models.Entities;
public class TrainingJob
{
    [Key] public int Id { get; set; }
    public int UserId { get; set; }
    [ForeignKey("UserId")] public virtual User User { get; set; } = null!;
    public int ModelId { get; set; }
    [ForeignKey("ModelId")] public virtual AIModel Model { get; set; } = null!;
    [Required, MaxLength(50)] public string Status { get; set; } = "Pending";
    [MaxLength(512)] public string? DatasetUrl { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Metrics { get; set; }
}