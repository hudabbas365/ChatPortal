using System.ComponentModel.DataAnnotations;

namespace ChatPortal.ViewModels;

public class ProfileViewModel
{
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    [DataType(DataType.Password)]
    public string? CurrentPassword { get; set; }

    [DataType(DataType.Password), MinLength(8)]
    public string? NewPassword { get; set; }

    [DataType(DataType.Password), Compare("NewPassword")]
    public string? ConfirmNewPassword { get; set; }
}
