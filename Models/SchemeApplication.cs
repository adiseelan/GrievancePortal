using System.ComponentModel.DataAnnotations;

namespace GrievancePortal.Models;

/// <summary>An applicant's application to a scheme, with workflow status.</summary>
public class SchemeApplication
{
    public int Id { get; set; }

    [Required]
    [MaxLength(40)]
    [Display(Name = "Application no.")]
    public string ApplicationNumber { get; set; } = string.Empty;

    [Required]
    public int SchemeId { get; set; }
    public Scheme? Scheme { get; set; }

    [Required]
    public string ApplicantId { get; set; } = string.Empty;
    public ApplicationUser? Applicant { get; set; }

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Draft;

    /// <summary>Server path to the uploaded supporting PDF (stored outside wwwroot).</summary>
    public string? SupportingDocumentPath { get; set; }

    [MaxLength(260)]
    public string? SupportingDocumentOriginalName { get; set; }

    /// <summary>Most recent officer remark (full history is in the audit trail).</summary>
    public string? OfficerRemarks { get; set; }

    public DateTime? SubmittedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
