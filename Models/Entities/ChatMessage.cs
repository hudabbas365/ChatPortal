using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatPortal.Models.Entities;

public class ChatMessage
{
    [Key]
    public int Id { get; set; }

    public int ChatSessionId { get; set; }

    [ForeignKey("ChatSessionId")]
    public virtual ChatSession ChatSession { get; set; } = null!;

    [Required, MaxLength(20)]
    public string Role { get; set; } = "User";

    [Required]
    public string Content { get; set; } = string.Empty;

    public int TokensUsed { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}