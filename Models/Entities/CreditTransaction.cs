using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatPortal.Models.Entities;

public class CreditTransaction
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    /// <summary>Positive = credits added; Negative = credits deducted</summary>
    public int Amount { get; set; }

    /// <summary>Purchase, QueryDeduction, Refund, Bonus</summary>
    [Required, MaxLength(50)]
    public string TransactionType { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? Description { get; set; }

    /// <summary>Optional reference to the data source that triggered a deduction</summary>
    public int? DataSourceId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
