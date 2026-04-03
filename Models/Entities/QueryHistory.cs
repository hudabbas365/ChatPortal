using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatPortal.Models.Entities;

public class QueryHistory
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    public int DataSourceId { get; set; }
    [ForeignKey("DataSourceId")]
    public virtual UserDataSource DataSource { get; set; } = null!;

    [Required]
    public string Query { get; set; } = string.Empty;

    public string? ResultJson { get; set; }

    public string? ChartDataJson { get; set; }

    public string? Narrative { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
