namespace GrievancePortal.Models;

/// <summary>Lifecycle of a grievance ticket.</summary>
public enum GrievanceStatus
{
    Open = 0,
    Assigned = 1,

    InProgress = 2,
    Resolved = 3,
    Closed = 4
}
