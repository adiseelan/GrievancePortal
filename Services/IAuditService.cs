using System.Security.Claims;
using GrievancePortal.Models;

namespace GrievancePortal.Services;

public interface IAuditService
{
    /// <summary>Records an action against an entity. Does not call SaveChanges.</summary>
    Task LogAsync(
        string entityType,
        int entityId,
        string action,
        ClaimsPrincipal performedBy,
        string? oldStatus = null,
        string? newStatus = null,
        string? remarks = null);

    /// <summary>Returns the audit trail for a given entity, newest first.</summary>
    Task<List<AuditLog>> GetTrailAsync(string entityType, int entityId);
}
