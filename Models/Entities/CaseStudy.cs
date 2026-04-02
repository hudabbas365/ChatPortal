using System.ComponentModel.DataAnnotations;

namespace ChatPortal.Models.Entities;

public class CaseStudy
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(256)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(256)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ClientName { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? ResultsSummary { get; set; }

    [MaxLength(512)]
    public string? CoverImageUrl { get; set; }

    public bool IsPublished { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}