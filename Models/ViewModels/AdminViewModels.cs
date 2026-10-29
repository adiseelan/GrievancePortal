using System.ComponentModel.DataAnnotations;

namespace GrievancePortal.Models.ViewModels;

public class UserRow
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public IList<string> RolesList { get; set; } = new List<string>();
    public DateTime CreatedAt { get; set; }
}

public class RoleAssignmentModel
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Primary role")]
    public string Role { get; set; } = string.Empty;
}

public class ReportsViewModel
{
    public Dictionary<ApplicationStatus, int> ApplicationCounts { get; set; } = new();
    public Dictionary<GrievanceStatus, int> GrievanceCounts { get; set; } = new();
    public int TotalSchemes { get; set; }
    public int ActiveSchemes { get; set; }
    public int TotalUsers { get; set; }
    public int TotalApplications { get; set; }
    public int TotalGrievances { get; set; }
}
