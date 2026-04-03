using System.ComponentModel.DataAnnotations;

namespace ChatPortal.Models.Entities;

public class ErrorLog
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(36)]
    public string RequestId { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ControllerName { get; set; }

    [MaxLength(100)]
    public string? ActionName { get; set; }

    [MaxLength(100)]
    public string? OrganizationName { get; set; }

    public string ErrorMessage { get; set; } = string.Empty;

    [MaxLength(500)]
    public string UserFriendlyMessage { get; set; } = string.Empty;

    public int? UserId { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [MaxLength(1000)]
    public string? RequestPath { get; set; }

    [MaxLength(10)]
    public string? HttpMethod { get; set; }
}
