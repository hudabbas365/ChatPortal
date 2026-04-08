using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatPortal.Models.Entities;

public class Dashboard
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [Required, MaxLength(256)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public bool IsPublic { get; set; } = false;

    public bool IsRevoked { get; set; } = false;

    [MaxLength(128)]
    public string PublicSlug { get; set; } = Guid.NewGuid().ToString("N");

    [MaxLength(512)]
    public string? EmbedToken { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Lock/unlock fields (Section 14)
    public bool IsLocked { get; set; } = false;
    [MaxLength(500)]
    public string? LockReason { get; set; }
    public DateTime? LockedAt { get; set; }
    public Guid? LockedByUserId { get; set; }
    [ForeignKey("LockedByUserId")]
    public virtual User? LockedByUser { get; set; }

    public virtual ICollection<PinnedChart> PinnedCharts { get; set; } = new List<PinnedChart>();
}
