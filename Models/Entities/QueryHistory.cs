using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatPortal.Models.Entities
{
    public class QueryHistory
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

        [Required]
        public int DataSourceConnectionId { get; set; }

        [ForeignKey("DataSourceConnectionId")]
        public virtual DataSourceConnection DataSourceConnection { get; set; } = null!;

        [Required]
        [MaxLength(4000)]
        public string Query { get; set; } = string.Empty;

        [MaxLength(50)]
        public string QueryType { get; set; } = "SELECT"; // SELECT, INSERT, UPDATE, DELETE, etc.

        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

        public int ExecutionTimeMs { get; set; }

        public int RowsAffected { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = string.Empty; // Success, Failed

        public string? ErrorMessage { get; set; }

        public string? ResultSnapshot { get; set; } // JSON snapshot of results (first 100 rows)

        public virtual ICollection<ChartDefinition> Charts { get; set; } = new List<ChartDefinition>();
    }
}
