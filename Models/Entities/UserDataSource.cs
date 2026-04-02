using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatPortal.Models.Entities;

public class UserDataSource
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [Required, MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Excel, CSV, SqlServer, Oracle</summary>
    [Required, MaxLength(50)]
    public string SourceType { get; set; } = string.Empty;

    /// <summary>File path for Excel/CSV; connection string for DB sources</summary>
    [MaxLength(2000)]
    public string? ConnectionDetails { get; set; }

    /// <summary>JSON array of selected table/sheet names</summary>
    public string? SelectedTables { get; set; }

    /// <summary>JSON schema snapshot of selected tables</summary>
    public string? SchemaSnapshot { get; set; }

    /// <summary>Server-side relative file path for uploaded files</summary>
    [MaxLength(1000)]
    public string? FilePath { get; set; }

    /// <summary>Active, Error, Pending</summary>
    [Required, MaxLength(50)]
    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
