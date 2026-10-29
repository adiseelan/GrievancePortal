using System.ComponentModel.DataAnnotations;

namespace GrievancePortal.Models;

/// <summary>Lifecycle of a scheme application.</summary>
public enum ApplicationStatus
{
    Draft = 0,
    Submitted = 1,

    [Display(Name = "Under Review")]
    UnderReview = 2,
    Approved = 3,
    Rejected = 4
}
