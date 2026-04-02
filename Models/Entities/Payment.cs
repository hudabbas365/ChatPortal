using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatPortal.Models.Entities;

public class Payment
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    public int? SubscriptionId { get; set; }

    [ForeignKey("SubscriptionId")]
    public virtual Subscription? Subscription { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [MaxLength(10)]
    public string Currency { get; set; } = "USD";

    [MaxLength(50)]
    public string? PaymentMethod { get; set; }

    [MaxLength(256)]
    public string? TransactionId { get; set; }

    [Required, MaxLength(50)]
    public string Status { get; set; } = "Pending";

    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
}