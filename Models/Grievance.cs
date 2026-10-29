using System.ComponentModel.DataAnnotations;

namespace GrievancePortal.Models;

/// <summary>A grievance raised by an applicant, tracked by a unique ticket number.</summary>
public class Grievance
{
    public int Id { get; set; }

    [Required]
    [MaxLength(40)]
    [Display(Name = "Ticket no.")]
    public string TicketNumber { get; set; } = string.Empty;

    [Required]
    public string ApplicantId { get; set; } = string.Empty;
    public ApplicationUser? Applicant { get; set; }

    [Required]
    [MaxLength(150)]
    public string Subject { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Category { get; set; }

    [Required]
    public string Description { get; set; } = string.Empty;

    public GrievanceStatus Status { get; set; } = GrievanceStatus.Open;

    public string? AssignedOfficerId { get; set; }
    public ApplicationUser? AssignedOfficer { get; set; }

    [Display(Name = "Resolution / response")]
    public string? Resolution { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
