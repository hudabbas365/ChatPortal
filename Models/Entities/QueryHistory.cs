using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class QueryHistory
{
    [Key]
    public int Id { get; set; }

        [Required]
    public int UserId { get; set; }

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;


    [Required]
        [MaxLength(4000)]
    public string Query { get; set; } = string.Empty;




}
