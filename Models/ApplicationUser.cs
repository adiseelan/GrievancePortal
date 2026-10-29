using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace GrievancePortal.Models;

/// <summary>Application user. Extends IdentityUser with a display name.</summary>
public class ApplicationUser : IdentityUser
{
    [Required]
    [MaxLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
