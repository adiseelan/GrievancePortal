using System.ComponentModel.DataAnnotations;

namespace GrievancePortal.Models;

/// <summary>Business details for an applicant. One profile per user.</summary>
public class EntrepreneurProfile
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    [Required]
    [MaxLength(150)]
    [Display(Name = "Business / enterprise name")]
    public string BusinessName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Display(Name = "Owner / proprietor name")]
    public string OwnerName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Business address")]
    public string Address { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string District { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    [Display(Name = "PIN code")]
    public string Pincode { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Phone]
    [Display(Name = "Contact phone")]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(50)]
    [Display(Name = "Udyam registration no. (optional)")]
    public string? UdyamNumber { get; set; }

    [MaxLength(100)]
    [Display(Name = "Type of business (optional)")]
    public string? BusinessType { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
