using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using GrievancePortal.Data;
using GrievancePortal.Models;

namespace GrievancePortal.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public AuditService(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public async Task LogAsync(
        string entityType,
        int entityId,
        string action,
        ClaimsPrincipal performedBy,
        string? oldStatus = null,
        string? newStatus = null,
        string? remarks = null)
    {
        var userId = _users.GetUserId(performedBy);
        var name = performedBy.Identity?.Name ?? "system";

        // Prefer the stored display name when available.
        if (userId is not null)
        {
            var user = await _users.FindByIdAsync(userId);
            if (user is not null && !string.IsNullOrWhiteSpace(user.FullName))
                name = user.FullName;
        }

        _db.AuditLogs.Add(new AuditLog
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            Remarks = remarks,
            PerformedById = userId,
            PerformedByName = name,
            Timestamp = DateTime.UtcNow
        });
    }

    public Task<List<AuditLog>> GetTrailAsync(string entityType, int entityId) =>
        _db.AuditLogs
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();
}
