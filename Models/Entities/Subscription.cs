using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatPortal.Models.Entities;

public class Subscription
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    public Guid PlanId { get; set; }

    [ForeignKey("PlanId")]
    public virtual Plan Plan { get; set; } = null!;

    public Guid? OrganizationId { get; set; }
    [ForeignKey("OrganizationId")]
    public virtual Organization? Organization { get; set; }

    [Required, MaxLength(50)]
    public string Status { get; set; } = "Active";

    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime EndDate { get; set; }
    public bool AutoRenew { get; set; } = true;
    public DateTime? CancelledAt { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}