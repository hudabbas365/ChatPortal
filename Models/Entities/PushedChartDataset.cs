using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatPortal.Models.Entities;

public class PushedChartDataset
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrganizationId { get; set; }
    [ForeignKey("OrganizationId")]
    public virtual Organization Organization { get; set; } = null!;

    public Guid UserId { get; set; }
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [Required, MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    public string JsonData { get; set; } = string.Empty;

    public Guid? SourceQueryHistoryId { get; set; }
    [ForeignKey("SourceQueryHistoryId")]
    public virtual QueryHistory? SourceQueryHistory { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
