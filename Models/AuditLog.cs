using System.ComponentModel.DataAnnotations;

namespace GrievancePortal.Models;

/// <summary>Immutable record of a status change / action (who, what, when).</summary>
public class AuditLog
{
    public int Id { get; set; }

    /// <summary>"Application" or "Grievance".</summary>
    [Required]
    [MaxLength(50)]
    public string EntityType { get; set; } = string.Empty;

    public int EntityId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? OldStatus { get; set; }

    [MaxLength(50)]
    public string? NewStatus { get; set; }

    public string? Remarks { get; set; }

    public string? PerformedById { get; set; }
    public ApplicationUser? PerformedBy { get; set; }

    [Required]
    [MaxLength(150)]
    public string PerformedByName { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
