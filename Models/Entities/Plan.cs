using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatPortal.Models.Entities;

public class Plan
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal MonthlyPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal AnnualPrice { get; set; }

    public string? Features { get; set; }

    public int MaxCredits { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}