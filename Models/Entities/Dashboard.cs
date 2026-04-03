using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatPortal.Models.Entities;

public class Dashboard
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }
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

    public virtual ICollection<PinnedChart> PinnedCharts { get; set; } = new List<PinnedChart>();
}
