using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatPortal.Models.Entities;

public class QueryHistory
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [Required]
    public int DataSourceId { get; set; }

    [ForeignKey("DataSourceId")]
    public virtual DataSource DataSource { get; set; } = null!;

    [Required]
    [MaxLength(4000)]
    public string Query { get; set; } = string.Empty;

    [MaxLength(50)]
    public string QueryType { get; set; } = "SELECT";

    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int ExecutionTimeMs { get; set; }

    public int RowsAffected { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public string? ResultSnapshot { get; set; }

    public string? ResultJson { get; set; }

    public string? ChartDataJson { get; set; }

    public string? Narrative { get; set; }

    public virtual ICollection<ChartDefinition> Charts { get; set; } = new List<ChartDefinition>();
}
