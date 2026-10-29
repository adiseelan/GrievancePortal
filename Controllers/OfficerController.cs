using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GrievancePortal.Data;
using GrievancePortal.Models;

namespace GrievancePortal.Controllers;

[Authorize(Roles = "Admin,Officer")]
public class OfficerController : Controller
{
    private readonly ApplicationDbContext _db;

    public OfficerController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Dashboard()
    {
        // Applications that need action: Submitted or Under Review.
        var pendingApplications = await _db.SchemeApplications
            .Include(a => a.Scheme)
            .Include(a => a.Applicant)
            .Where(a => a.Status == ApplicationStatus.Submitted || a.Status == ApplicationStatus.UnderReview)
            .OrderBy(a => a.SubmittedAt)
            .ToListAsync();

        // Grievances that need action: not yet resolved/closed.
        var openGrievances = await _db.Grievances
            .Include(g => g.Applicant)
            .Include(g => g.AssignedOfficer)
            .Where(g => g.Status != GrievanceStatus.Resolved && g.Status != GrievanceStatus.Closed)
            .OrderBy(g => g.CreatedAt)
            .ToListAsync();

        ViewBag.PendingApplications = pendingApplications;
        ViewBag.OpenGrievances = openGrievances;
        ViewBag.ApprovedCount = await _db.SchemeApplications.CountAsync(a => a.Status == ApplicationStatus.Approved);
        ViewBag.ResolvedCount = await _db.Grievances.CountAsync(g => g.Status == GrievanceStatus.Resolved || g.Status == GrievanceStatus.Closed);

        return View();
    }
}
