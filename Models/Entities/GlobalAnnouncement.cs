using System.ComponentModel.DataAnnotations;

namespace ChatPortal.Models.Entities;

public class GlobalAnnouncement
{
    [Key] public int Id { get; set; }

    [Required, MaxLength(256)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>informational | warning | urgent</summary>
    [MaxLength(50)]
    public string Priority { get; set; } = "informational";

    public bool IsActive { get; set; } = true;

    public int CreatedById { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
