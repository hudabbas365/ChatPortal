using System.ComponentModel.DataAnnotations;

namespace ChatPortal.Models.Entities;

public class Partner
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? LogoUrl { get; set; }

    [MaxLength(512)]
    public string? WebsiteUrl { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}