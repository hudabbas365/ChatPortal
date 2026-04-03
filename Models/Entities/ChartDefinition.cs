using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatPortal.Models.Entities
{
    public class ChartDefinition
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        public int? OrganizationId { get; set; }

        [ForeignKey("OrganizationId")]
        public virtual Organization? Organization { get; set; }

        public int? QueryHistoryId { get; set; }

        [ForeignKey("QueryHistoryId")]
        public virtual QueryHistory? QueryHistory { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string ChartType { get; set; } = "bar"; // bar, line, pie, doughnut, scatter, radar, polarArea

        [Required]
        public string DataConfig { get; set; } = "{}"; // JSON: { labels: [], datasets: [], options: {} }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public bool IsPinned { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }
    }
}
