using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace GrievancePortal.Models.ViewModels;

public class ApplyViewModel
{
    [Required]
    public int SchemeId { get; set; }

    public string SchemeName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Eligibility { get; set; } = string.Empty;

    [Display(Name = "Supporting document (PDF)")]
    public IFormFile? SupportingDocument { get; set; }
}

public class ApplicationDetailsViewModel
{
    public SchemeApplication Application { get; set; } = default!;
    public List<AuditLog> Trail { get; set; } = new();

    /// <summary>True when the current viewer is staff (officer/admin).</summary>
    public bool IsStaffView { get; set; }
}

public class ApplicationStatusUpdateModel
{
    [Required]
    public int Id { get; set; }

    [Required]
    [Display(Name = "New status")]
    public ApplicationStatus NewStatus { get; set; }

    [Display(Name = "Remarks")]
    [MaxLength(2000)]
    public string? Remarks { get; set; }
}
