using System.ComponentModel.DataAnnotations;

namespace GrievancePortal.Models.ViewModels;

public class SchemeFormModel
{
    public int Id { get; set; }

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Department")]
    [MaxLength(120)]
    public string Department { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Eligibility criteria")]
    public string Eligibility { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Active (visible to the public)")]
    public bool IsActive { get; set; } = true;

    public static SchemeFormModel FromEntity(Scheme s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        Department = s.Department,
        Eligibility = s.Eligibility,
        Description = s.Description,
        IsActive = s.IsActive
    };
}
