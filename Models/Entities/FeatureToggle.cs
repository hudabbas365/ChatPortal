using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatPortal.Models.Entities;

public class FeatureToggle
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(256)]
    public string? Description { get; set; }

    public bool IsEnabled { get; set; } = false;

    /// <summary>Comma-separated role names that can see this feature, or null for all.</summary>
    [MaxLength(500)]
    public string? AllowedRoles { get; set; }

    /// <summary>Optional team scope — null means tenant-wide.</summary>
    public int? TeamId { get; set; }
    [ForeignKey("TeamId")]
    public virtual Team? Team { get; set; }

    public int CreatedByAdminId { get; set; }
    [ForeignKey("CreatedByAdminId")]
    public virtual User CreatedByAdmin { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
