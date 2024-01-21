using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PasswordManager.Api;

public class UserPassword
{
    [Key]
    public int Id { get; set; }
    [ForeignKey(nameof(IdentityUser))]
    public string UserId { get; set; } = null!;
    public IdentityUser User { get; set; } = null!;
    [Required(ErrorMessage = "Encrypted password is a required field.")]
    public string? EncrptedPassword { get; set; }
}
