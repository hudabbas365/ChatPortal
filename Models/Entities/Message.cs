using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ChatPortal.Models.Entities;
public class Message
{
    [Key] public int Id { get; set; }
    public int SenderId { get; set; }
    [ForeignKey("SenderId")] public virtual User Sender { get; set; } = null!;
    public int ReceiverId { get; set; }
    [ForeignKey("ReceiverId")] public virtual User Receiver { get; set; } = null!;
    [Required] public string Content { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}