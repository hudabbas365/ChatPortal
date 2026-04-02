using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatPortal.Models.Entities;

public class PaymentTransaction
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    public int? CreditPackageId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    /// <summary>Stripe, PayPal</summary>
    [Required, MaxLength(50)]
    public string Provider { get; set; } = string.Empty;

    /// <summary>Pending, Completed, Failed, Cancelled</summary>
    [Required, MaxLength(50)]
    public string Status { get; set; } = "Pending";

    [MaxLength(512)]
    public string? ProviderTransactionId { get; set; }

    [MaxLength(512)]
    public string? ProviderSessionId { get; set; }

    public int? CreditsAwarded { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
