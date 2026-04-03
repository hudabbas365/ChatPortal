using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatPortal.Models.Entities
{
    public class DataSourceConnection
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

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string SourceType { get; set; } = string.Empty; // SQL, NoSQL, CloudStorage, etc.

        [Required, MaxLength(100)]
        public string Provider { get; set; } = string.Empty; // MySQL, PostgreSQL, S3, etc.

        [MaxLength(500)]
        public string? ConnectionString { get; set; }

        [MaxLength(500)]
        public string? ApiEndpoint { get; set; }

        [MaxLength(500)]
        public string? ApiKey { get; set; }

        [MaxLength(500)]
        public string? AccessToken { get; set; }

        [MaxLength(200)]
        public string? Username { get; set; }

        [MaxLength(500)]
        public string? PasswordHash { get; set; }

        public string? AdditionalConfig { get; set; } // JSON for extra configuration

        public bool IsActive { get; set; } = true;

        public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastSyncAt { get; set; }

        public string? LastSyncStatus { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }
    }
}
