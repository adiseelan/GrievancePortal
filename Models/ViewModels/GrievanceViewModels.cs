using System.ComponentModel.DataAnnotations;

namespace GrievancePortal.Models.ViewModels;

public class GrievanceFormModel
{
    [Required]
    [MaxLength(150)]
    public string Subject { get; set; } = string.Empty;

    [MaxLength(100)]
    [Display(Name = "Category (optional)")]
    public string? Category { get; set; }

    [Required]
    [Display(Name = "Describe your grievance")]
    public string Description { get; set; } = string.Empty;
}

public class GrievanceDetailsViewModel
{
    public Grievance Grievance { get; set; } = default!;
    public List<AuditLog> Trail { get; set; } = new();
    public bool IsStaffView { get; set; }
}

public class GrievanceUpdateModel
{
    [Required]
    public int Id { get; set; }

    [Required]
    [Display(Name = "New status")]
    public GrievanceStatus NewStatus { get; set; }

    [Display(Name = "Assign to officer")]
    public string? AssignedOfficerId { get; set; }

    [Display(Name = "Resolution / response")]
    [MaxLength(2000)]
    public string? Resolution { get; set; }
}

public class TrackGrievanceModel
{
    [Display(Name = "Ticket number")]
    public string? TicketNumber { get; set; }

    public Grievance? Result { get; set; }
    public bool Searched { get; set; }
}
