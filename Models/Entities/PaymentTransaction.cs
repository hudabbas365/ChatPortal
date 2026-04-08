using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatPortal.Models.Entities;

public class PaymentTransaction
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    public Guid? PlanId { get; set; }
    [ForeignKey("PlanId")]
    public virtual Plan? Plan { get; set; }

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

    [MaxLength(256)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
