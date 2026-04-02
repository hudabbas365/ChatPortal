using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ChatPortal.Models.Entities;
public class SentimentResult
{
    [Key] public int Id { get; set; }
    public int UserId { get; set; }
    [ForeignKey("UserId")] public virtual User User { get; set; } = null!;
    [Required] public string InputText { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string Sentiment { get; set; } = "Neutral";
    public double Score { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}