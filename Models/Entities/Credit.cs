using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatPortal.Models.Entities;

public class Credit
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    public int Amount { get; set; }

    [Required, MaxLength(20)]
    public string Type { get; set; } = "Purchased";

    [MaxLength(256)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}