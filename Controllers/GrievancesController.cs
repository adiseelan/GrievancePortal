using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GrievancePortal.Data;
using GrievancePortal.Models;
using GrievancePortal.Models.ViewModels;
using GrievancePortal.Services;

namespace GrievancePortal.Controllers;

[Authorize]
public class GrievancesController : Controller
{
    private const string EntityType = "Grievance";

    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _audit;

    public GrievancesController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IAuditService audit)
    {
        _db = db;
        _userManager = userManager;
        _audit = audit;
    }

    private string UserId => _userManager.GetUserId(User)!;
    private bool IsStaff => User.IsInRole(Roles.Admin) || User.IsInRole(Roles.Officer);

    // ---- Applicant: my grievances ----
    [Authorize(Roles = Roles.Applicant)]
    public async Task<IActionResult> Index()
    {
        var mine = await _db.Grievances
            .Where(g => g.ApplicantId == UserId)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();

        return View(mine);
    }

    // ---- Applicant: raise a grievance ----
    [Authorize(Roles = Roles.Applicant)]
    [HttpGet]
    public IActionResult Create() => View(new GrievanceFormModel());

    [Authorize(Roles = Roles.Applicant)]
    [HttpPost]
    public async Task<IActionResult> Create(GrievanceFormModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var now = DateTime.UtcNow;
        var grievance = new Grievance
        {
            TicketNumber = NewTicketNumber(),
            ApplicantId = UserId,
            Subject = model.Subject.Trim(),
            Category = model.Category?.Trim(),
            Description = model.Description.Trim(),
            Status = GrievanceStatus.Open,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Grievances.Add(grievance);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(EntityType, grievance.Id, "Grievance filed", User,
            newStatus: GrievanceStatus.Open.ToString());
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Grievance filed. Your ticket number is {grievance.TicketNumber}.";
        return RedirectToAction(nameof(Details), new { id = grievance.Id });
    }

    // ---- Owner or staff: details ----
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var grievance = await _db.Grievances
            .Include(g => g.Applicant)
            .Include(g => g.AssignedOfficer)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (grievance is null)
            return NotFound();

        if (grievance.ApplicantId != UserId && !IsStaff)
            return Forbid();

        if (IsStaff)
            ViewBag.Officers = await OfficerSelectListAsync(grievance.AssignedOfficerId);

        var vm = new GrievanceDetailsViewModel
        {
            Grievance = grievance,
            Trail = await _audit.GetTrailAsync(EntityType, id),
            IsStaffView = IsStaff
        };

        return View(vm);
    }

    // ---- Staff: assign / update status / resolve ----
    [Authorize(Roles = "Admin,Officer")]
    [HttpPost]
    public async Task<IActionResult> Update(GrievanceUpdateModel model)
    {
        var grievance = await _db.Grievances
            .Include(g => g.Applicant)
            .FirstOrDefaultAsync(g => g.Id == model.Id);

        if (grievance is null)
            return NotFound();

        if ((model.NewStatus == GrievanceStatus.Resolved || model.NewStatus == GrievanceStatus.Closed)
            && string.IsNullOrWhiteSpace(model.Resolution)
            && string.IsNullOrWhiteSpace(grievance.Resolution))
        {
            TempData["Error"] = "A resolution note is required to resolve or close a grievance.";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        var old = grievance.Status;

        // Validate assigned officer, if provided.
        if (!string.IsNullOrEmpty(model.AssignedOfficerId))
        {
            var officer = await _userManager.FindByIdAsync(model.AssignedOfficerId);
            if (officer is null || !await _userManager.IsInRoleAsync(officer, Roles.Officer))
            {
                TempData["Error"] = "The selected assignee is not a valid officer.";
                return RedirectToAction(nameof(Details), new { id = model.Id });
            }
            grievance.AssignedOfficerId = model.AssignedOfficerId;
        }

        grievance.Status = model.NewStatus;
        if (!string.IsNullOrWhiteSpace(model.Resolution))
            grievance.Resolution = model.Resolution.Trim();
        grievance.UpdatedAt = DateTime.UtcNow;

        await _audit.LogAsync(EntityType, grievance.Id, "Grievance updated", User,
            oldStatus: old.ToString(),
            newStatus: model.NewStatus.ToString(),
            remarks: model.Resolution?.Trim());

        await _db.SaveChangesAsync();

        TempData["Success"] = $"Grievance {grievance.TicketNumber} updated to {model.NewStatus}.";
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    // ---- Public: track a grievance by ticket number ----
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Track() => View(new TrackGrievanceModel());

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Track(TrackGrievanceModel model)
    {
        model.Searched = true;

        if (!string.IsNullOrWhiteSpace(model.TicketNumber))
        {
            var ticket = model.TicketNumber.Trim();
            model.Result = await _db.Grievances
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.TicketNumber == ticket);
        }

        return View(model);
    }

    private async Task<SelectList> OfficerSelectListAsync(string? selectedId)
    {
        var officers = await _userManager.GetUsersInRoleAsync(Roles.Officer);
        var items = officers
            .OrderBy(o => o.FullName)
            .Select(o => new { o.Id, Label = $"{o.FullName} ({o.Email})" })
            .ToList();

        return new SelectList(items, "Id", "Label", selectedId);
    }

    private static string NewTicketNumber() =>
        $"GRV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
}
