using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatPortal.Models.Entities;

public class ErrorLog
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(36)]
    public string RequestId { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ControllerName { get; set; }

    [MaxLength(100)]
    public string? ActionName { get; set; }

    [MaxLength(100)]
    public string? OrganizationName { get; set; }

    public Guid? OrganizationId { get; set; }
    [ForeignKey("OrganizationId")]
    public virtual Organization? Organization { get; set; }

    public string ErrorMessage { get; set; } = string.Empty;

    [MaxLength(500)]
    public string UserFriendlyMessage { get; set; } = string.Empty;

    public Guid? UserId { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [MaxLength(1000)]
    public string? RequestPath { get; set; }

    [MaxLength(10)]
    public string? HttpMethod { get; set; }
}
