using System.ComponentModel.DataAnnotations;

namespace GrievancePortal.Models;

/// <summary>A government scheme that entrepreneurs can apply to.</summary>
public class Scheme
{
    public int Id { get; set; }

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Eligibility criteria")]
    public string Eligibility { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string Department { get; set; } = string.Empty;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SchemeApplication> Applications { get; set; } = new List<SchemeApplication>();
}
