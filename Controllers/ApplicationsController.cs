using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GrievancePortal.Data;
using GrievancePortal.Models;
using GrievancePortal.Models.ViewModels;
using GrievancePortal.Services;

namespace GrievancePortal.Controllers;

[Authorize]
public class ApplicationsController : Controller
{
    private const string EntityType = "Application";

    private static readonly Dictionary<ApplicationStatus, ApplicationStatus[]> AllowedTransitions = new()
    {
        [ApplicationStatus.Draft] = Array.Empty<ApplicationStatus>(),
        [ApplicationStatus.Submitted] = new[] { ApplicationStatus.UnderReview, ApplicationStatus.Approved, ApplicationStatus.Rejected },
        [ApplicationStatus.UnderReview] = new[] { ApplicationStatus.Approved, ApplicationStatus.Rejected },
        [ApplicationStatus.Approved] = Array.Empty<ApplicationStatus>(),
        [ApplicationStatus.Rejected] = Array.Empty<ApplicationStatus>()
    };

    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IFileStorageService _files;
    private readonly IAuditService _audit;
    private readonly IApplicationEmailSender _email;

    public ApplicationsController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IFileStorageService files,
        IAuditService audit,
        IApplicationEmailSender email)
    {
        _db = db;
        _userManager = userManager;
        _files = files;
        _audit = audit;
        _email = email;
    }

    private string UserId => _userManager.GetUserId(User)!;
    private bool IsStaff => User.IsInRole(Roles.Admin) || User.IsInRole(Roles.Officer);

    // ---- Applicant: my applications ----
    [Authorize(Roles = Roles.Applicant)]
    public async Task<IActionResult> Index()
    {
        var mine = await _db.SchemeApplications
            .Include(a => a.Scheme)
            .Where(a => a.ApplicantId == UserId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return View(mine);
    }

    // ---- Applicant: apply to a scheme ----
    [Authorize(Roles = Roles.Applicant)]
    [HttpGet]
    public async Task<IActionResult> Apply(int schemeId)
    {
        var scheme = await _db.Schemes.FirstOrDefaultAsync(s => s.Id == schemeId && s.IsActive);
        if (scheme is null)
            return NotFound();

        // An applicant must complete their business profile before applying.
        var hasProfile = await _db.EntrepreneurProfiles.AnyAsync(p => p.UserId == UserId);
        if (!hasProfile)
        {
            TempData["Info"] = "Please complete your business profile before applying to a scheme.";
            return RedirectToAction("Edit", "Profile");
        }

        return View(new ApplyViewModel
        {
            SchemeId = scheme.Id,
            SchemeName = scheme.Name,
            Department = scheme.Department,
            Eligibility = scheme.Eligibility
        });
    }

    [Authorize(Roles = Roles.Applicant)]
    [HttpPost]
    [RequestSizeLimit(20_000_000)] // 20 MB request ceiling; per-file limit enforced in the service
    public async Task<IActionResult> Apply(ApplyViewModel model, string submitAction)
    {
        var scheme = await _db.Schemes.FirstOrDefaultAsync(s => s.Id == model.SchemeId && s.IsActive);
        if (scheme is null)
            return NotFound();

        model.SchemeName = scheme.Name;
        model.Department = scheme.Department;
        model.Eligibility = scheme.Eligibility;

        var hasProfile = await _db.EntrepreneurProfiles.AnyAsync(p => p.UserId == UserId);
        if (!hasProfile)
        {
            TempData["Info"] = "Please complete your business profile before applying to a scheme.";
            return RedirectToAction("Edit", "Profile");
        }

        var submitNow = string.Equals(submitAction, "submit", StringComparison.OrdinalIgnoreCase);

        // A PDF is required to submit; optional when only saving a draft.
        if (submitNow && (model.SupportingDocument is null || model.SupportingDocument.Length == 0))
            ModelState.AddModelError(nameof(model.SupportingDocument), "A supporting PDF is required to submit the application.");

        if (!ModelState.IsValid)
            return View(model);

        string? storedPath = null;
        string? originalName = null;

        if (model.SupportingDocument is { Length: > 0 })
        {
            try
            {
                var stored = await _files.SaveAsync(model.SupportingDocument, "applications");
                storedPath = stored.StoredPath;
                originalName = stored.OriginalName;
            }
            catch (FileValidationException ex)
            {
                ModelState.AddModelError(nameof(model.SupportingDocument), ex.Message);
                return View(model);
            }
        }

        var now = DateTime.UtcNow;
        var application = new SchemeApplication
        {
            ApplicationNumber = NewApplicationNumber(),
            SchemeId = scheme.Id,
            ApplicantId = UserId,
            Status = submitNow ? ApplicationStatus.Submitted : ApplicationStatus.Draft,
            SupportingDocumentPath = storedPath,
            SupportingDocumentOriginalName = originalName,
            SubmittedAt = submitNow ? now : null,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.SchemeApplications.Add(application);
        await _db.SaveChangesAsync(); // assigns application.Id

        await _audit.LogAsync(
            EntityType, application.Id,
            submitNow ? "Application submitted" : "Application saved as draft",
            User,
            oldStatus: null,
            newStatus: application.Status.ToString());
        await _db.SaveChangesAsync();

        TempData["Success"] = submitNow
            ? $"Application {application.ApplicationNumber} submitted."
            : $"Draft {application.ApplicationNumber} saved.";

        return RedirectToAction(nameof(Details), new { id = application.Id });
    }

    // ---- Applicant: submit an existing draft ----
    [Authorize(Roles = Roles.Applicant)]
    [HttpPost]
    public async Task<IActionResult> Submit(int id)
    {
        var application = await _db.SchemeApplications.FirstOrDefaultAsync(a => a.Id == id);
        if (application is null)
            return NotFound();

        if (application.ApplicantId != UserId)
            return Forbid();

        if (application.Status != ApplicationStatus.Draft)
        {
            TempData["Error"] = "Only draft applications can be submitted.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (string.IsNullOrEmpty(application.SupportingDocumentPath))
        {
            TempData["Error"] = "Please attach a supporting PDF before submitting. Create the application again with a document, or contact the office.";
            return RedirectToAction(nameof(Details), new { id });
        }

        application.Status = ApplicationStatus.Submitted;
        application.SubmittedAt = DateTime.UtcNow;
        application.UpdatedAt = DateTime.UtcNow;

        await _audit.LogAsync(EntityType, application.Id, "Application submitted", User,
            oldStatus: ApplicationStatus.Draft.ToString(),
            newStatus: ApplicationStatus.Submitted.ToString());

        await _db.SaveChangesAsync();

        TempData["Success"] = $"Application {application.ApplicationNumber} submitted.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // ---- Owner or staff: details + audit trail ----
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var application = await _db.SchemeApplications
            .Include(a => a.Scheme)
            .Include(a => a.Applicant)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (application is null)
            return NotFound();

        if (application.ApplicantId != UserId && !IsStaff)
            return Forbid();

        var vm = new ApplicationDetailsViewModel
        {
            Application = application,
            Trail = await _audit.GetTrailAsync(EntityType, id),
            IsStaffView = IsStaff
        };

        return View(vm);
    }

    // ---- Staff: change status + remark ----
    [Authorize(Roles = "Admin,Officer")]
    [HttpPost]
    public async Task<IActionResult> UpdateStatus(ApplicationStatusUpdateModel model)
    {
        var application = await _db.SchemeApplications
            .Include(a => a.Applicant)
            .FirstOrDefaultAsync(a => a.Id == model.Id);

        if (application is null)
            return NotFound();

        var allowed = AllowedTransitions.TryGetValue(application.Status, out var targets) ? targets : Array.Empty<ApplicationStatus>();
        if (!allowed.Contains(model.NewStatus))
        {
            TempData["Error"] = $"Cannot change status from {application.Status} to {model.NewStatus}.";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        if (model.NewStatus == ApplicationStatus.Rejected && string.IsNullOrWhiteSpace(model.Remarks))
        {
            TempData["Error"] = "A remark is required when rejecting an application.";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        var old = application.Status;
        application.Status = model.NewStatus;
        if (!string.IsNullOrWhiteSpace(model.Remarks))
            application.OfficerRemarks = model.Remarks.Trim();
        application.UpdatedAt = DateTime.UtcNow;

        await _audit.LogAsync(EntityType, application.Id, "Status changed", User,
            oldStatus: old.ToString(),
            newStatus: model.NewStatus.ToString(),
            remarks: model.Remarks?.Trim());

        await _db.SaveChangesAsync();

        // Optional notification (no-op unless Email:Enabled = true).
        if (application.Applicant?.Email is { Length: > 0 } toEmail)
        {
            await _email.SendAsync(
                toEmail,
                $"Update on your application {application.ApplicationNumber}",
                $"<p>Dear {application.Applicant.FullName},</p>" +
                $"<p>The status of your application <strong>{application.ApplicationNumber}</strong> has changed to <strong>{model.NewStatus}</strong>.</p>" +
                (string.IsNullOrWhiteSpace(model.Remarks) ? "" : $"<p>Remarks: {model.Remarks}</p>") +
                "<p>You can track it anytime by signing in to the portal.</p>");
        }

        TempData["Success"] = $"Application moved to {model.NewStatus}.";
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    // ---- Owner or staff: download the protected supporting document ----
    [HttpGet]
    public async Task<IActionResult> Document(int id)
    {
        var application = await _db.SchemeApplications.FirstOrDefaultAsync(a => a.Id == id);
        if (application is null)
            return NotFound();

        if (application.ApplicantId != UserId && !IsStaff)
            return Forbid();

        if (string.IsNullOrEmpty(application.SupportingDocumentPath))
            return NotFound();

        var opened = _files.OpenRead(application.SupportingDocumentPath);
        if (opened is null)
            return NotFound();

        var downloadName = application.SupportingDocumentOriginalName ?? $"{application.ApplicationNumber}.pdf";
        return File(opened.Value.Stream, opened.Value.ContentType, downloadName);
    }

    private static string NewApplicationNumber() =>
        $"APP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
}
