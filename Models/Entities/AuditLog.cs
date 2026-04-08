using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatPortal.Models.Entities;

public class AuditLog
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ActorUserId { get; set; }
    [ForeignKey("ActorUserId")]
    public virtual User ActorUser { get; set; } = null!;

    [Required, MaxLength(256)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? TargetEntityType { get; set; }

    public Guid? TargetEntityId { get; set; }

    public string? Details { get; set; }

    [MaxLength(50)]
    public string? IpAddress { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
