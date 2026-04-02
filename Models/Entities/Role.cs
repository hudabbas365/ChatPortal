using System.ComponentModel.DataAnnotations;

namespace ChatPortal.Models.Entities;

public class Role
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(256)]
    public string? Description { get; set; }

    public string? Permissions { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}