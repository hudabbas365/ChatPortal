using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatPortal.Models.Entities;

public class Plan
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal MonthlyPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal AnnualPrice { get; set; }

    public string? Features { get; set; }

    /// <summary>Maximum number of workspaces. -1 = unlimited.</summary>
    public int MaxWorkspaces { get; set; } = 3;

    /// <summary>Maximum number of charts. -1 = unlimited.</summary>
    public int MaxCharts { get; set; } = 10;

    public bool IsActive { get; set; } = true;

    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}