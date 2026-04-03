using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatPortal.Models.Entities;

public class PinnedChart
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    public int QueryHistoryId { get; set; }
    [ForeignKey("QueryHistoryId")]
    public virtual QueryHistory QueryHistory { get; set; } = null!;

    public int? DashboardId { get; set; }
    [ForeignKey("DashboardId")]
    public virtual Dashboard? Dashboard { get; set; }

    [Required, MaxLength(256)]
    public string Title { get; set; } = string.Empty;

    public string? ChartDataJson { get; set; }

    public int Position { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
