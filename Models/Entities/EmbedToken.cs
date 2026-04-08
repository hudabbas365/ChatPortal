using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatPortal.Models.Entities;

public class EmbedToken
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DashboardId { get; set; }
    [ForeignKey("DashboardId")]
    public virtual Dashboard Dashboard { get; set; } = null!;

    [Required, MaxLength(1024)]
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public Guid CreatedBy { get; set; }
    [ForeignKey("CreatedBy")]
    public virtual User Creator { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
