using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatPortal.Models.Entities;

public class Subscription
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    public int PlanId { get; set; }

    [ForeignKey("PlanId")]
    public virtual Plan Plan { get; set; } = null!;

    [Required, MaxLength(50)]
    public string Status { get; set; } = "Active";

    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime EndDate { get; set; }
    public bool AutoRenew { get; set; } = true;
    public DateTime? CancelledAt { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}