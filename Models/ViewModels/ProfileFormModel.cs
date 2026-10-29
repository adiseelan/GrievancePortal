using System.ComponentModel.DataAnnotations;

namespace GrievancePortal.Models.ViewModels;

public class ProfileFormModel
{
    [Required]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Enter a valid business name (at least 2 characters).")]
    [Display(Name = "Business / enterprise name")]
    public string BusinessName { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Enter a valid owner name (at least 2 characters).")]
    [Display(Name = "Owner / proprietor name")]
    public string OwnerName { get; set; } = string.Empty;

    [Required]
    [StringLength(250, MinimumLength = 5, ErrorMessage = "Enter a valid address (at least 5 characters).")]
    [Display(Name = "Business address")]
    public string Address { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Enter a valid district.")]
    public string District { get; set; } = string.Empty;

    // Indian PIN code: exactly 6 digits, first digit 1-9 (0 is not a valid leading digit).
    [Required]
    [RegularExpression(@"^[1-9][0-9]{5}$", ErrorMessage = "Enter a valid 6-digit PIN code.")]
    [Display(Name = "PIN code")]
    public string Pincode { get; set; } = string.Empty;

    // Indian mobile number: exactly 10 digits, starting 6, 7, 8 or 9.
    [Required]
    [RegularExpression(@"^[6-9][0-9]{9}$", ErrorMessage = "Enter a valid 10-digit mobile number (starting 6-9).")]
    [Display(Name = "Contact phone")]
    public string Phone { get; set; } = string.Empty;

    // Udyam number (optional). Official format, e.g. UDYAM-TN-00-1234567.
    // Validation only applies when a value is entered.
    [RegularExpression(@"^UDYAM-[A-Z]{2}-[0-9]{2}-[0-9]{7}$",
        ErrorMessage = "Udyam number must look like UDYAM-TN-00-1234567.")]
    [Display(Name = "Udyam registration no. (optional)")]
    public string? UdyamNumber { get; set; }

    [StringLength(100)]
    [Display(Name = "Type of business (optional)")]
    public string? BusinessType { get; set; }

    public bool HasExistingProfile { get; set; }
}
