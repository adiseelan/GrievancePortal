using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GrievancePortal.Data;
using GrievancePortal.Models;
using GrievancePortal.Models.ViewModels;

namespace GrievancePortal.Controllers;

[Authorize(Roles = Roles.Admin)]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Dashboard()
    {
        ViewBag.TotalUsers = await _userManager.Users.CountAsync();
        ViewBag.TotalSchemes = await _db.Schemes.CountAsync();
        ViewBag.ActiveSchemes = await _db.Schemes.CountAsync(s => s.IsActive);
        ViewBag.TotalApplications = await _db.SchemeApplications.CountAsync();
        ViewBag.PendingApplications = await _db.SchemeApplications
            .CountAsync(a => a.Status == ApplicationStatus.Submitted || a.Status == ApplicationStatus.UnderReview);
        ViewBag.TotalGrievances = await _db.Grievances.CountAsync();
        ViewBag.OpenGrievances = await _db.Grievances
            .CountAsync(g => g.Status != GrievanceStatus.Resolved && g.Status != GrievanceStatus.Closed);

        return View();
    }

    // ---- User management ----
    public async Task<IActionResult> Users()
    {
        var users = await _userManager.Users.OrderBy(u => u.FullName).ToListAsync();
        var rows = new List<UserRow>();

        foreach (var u in users)
        {
            rows.Add(new UserRow
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email ?? "",
                RolesList = await _userManager.GetRolesAsync(u),
                CreatedAt = u.CreatedAt
            });
        }

        ViewBag.AllRoles = Roles.All;
        return View(rows);
    }

    [HttpPost]
    public async Task<IActionResult> SetRole(RoleAssignmentModel model)
    {
        if (!Roles.All.Contains(model.Role))
        {
            TempData["Error"] = "Unknown role.";
            return RedirectToAction(nameof(Users));
        }

        var user = await _userManager.FindByIdAsync(model.UserId);
        if (user is null)
            return NotFound();

        // An admin cannot change their own role (prevents accidental lock-out).
        if (user.Id == _userManager.GetUserId(User))
        {
            TempData["Error"] = "You cannot change your own role.";
            return RedirectToAction(nameof(Users));
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, model.Role);

        TempData["Success"] = $"{user.FullName} is now {model.Role}.";
        return RedirectToAction(nameof(Users));
    }

    // ---- Reports ----
    public async Task<IActionResult> Reports()
    {
        var appCounts = Enum.GetValues<ApplicationStatus>()
            .ToDictionary(s => s, s => 0);
        foreach (var g in await _db.SchemeApplications.GroupBy(a => a.Status)
                     .Select(x => new { x.Key, Count = x.Count() }).ToListAsync())
            appCounts[g.Key] = g.Count;

        var grvCounts = Enum.GetValues<GrievanceStatus>()
            .ToDictionary(s => s, s => 0);
        foreach (var g in await _db.Grievances.GroupBy(a => a.Status)
                     .Select(x => new { x.Key, Count = x.Count() }).ToListAsync())
            grvCounts[g.Key] = g.Count;

        var vm = new ReportsViewModel
        {
            ApplicationCounts = appCounts,
            GrievanceCounts = grvCounts,
            TotalSchemes = await _db.Schemes.CountAsync(),
            ActiveSchemes = await _db.Schemes.CountAsync(s => s.IsActive),
            TotalUsers = await _userManager.Users.CountAsync(),
            TotalApplications = await _db.SchemeApplications.CountAsync(),
            TotalGrievances = await _db.Grievances.CountAsync()
        };

        return View(vm);
    }

    public async Task<IActionResult> ExportApplicationsCsv()
    {
        var apps = await _db.SchemeApplications
            .Include(a => a.Scheme)
            .Include(a => a.Applicant)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("ApplicationNumber,Scheme,Department,Applicant,Email,Status,SubmittedAt,CreatedAt");
        foreach (var a in apps)
        {
            sb.AppendLine(string.Join(",",
                Csv(a.ApplicationNumber),
                Csv(a.Scheme?.Name),
                Csv(a.Scheme?.Department),
                Csv(a.Applicant?.FullName),
                Csv(a.Applicant?.Email),
                Csv(a.Status.ToString()),
                Csv(a.SubmittedAt?.ToString("u")),
                Csv(a.CreatedAt.ToString("u"))));
        }

        return CsvFile(sb, $"applications-{DateTime.UtcNow:yyyyMMdd-HHmm}.csv");
    }

    public async Task<IActionResult> ExportGrievancesCsv()
    {
        var items = await _db.Grievances
            .Include(g => g.Applicant)
            .Include(g => g.AssignedOfficer)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("TicketNumber,Subject,Category,Applicant,Email,Status,AssignedOfficer,CreatedAt,UpdatedAt");
        foreach (var g in items)
        {
            sb.AppendLine(string.Join(",",
                Csv(g.TicketNumber),
                Csv(g.Subject),
                Csv(g.Category),
                Csv(g.Applicant?.FullName),
                Csv(g.Applicant?.Email),
                Csv(g.Status.ToString()),
                Csv(g.AssignedOfficer?.FullName),
                Csv(g.CreatedAt.ToString("u")),
                Csv(g.UpdatedAt.ToString("u"))));
        }

        return CsvFile(sb, $"grievances-{DateTime.UtcNow:yyyyMMdd-HHmm}.csv");
    }

    private FileContentResult CsvFile(StringBuilder sb, string fileName)
    {
        // UTF-8 BOM so Excel opens non-ASCII correctly.
        var preamble = Encoding.UTF8.GetPreamble();
        var body = Encoding.UTF8.GetBytes(sb.ToString());
        var bytes = new byte[preamble.Length + body.Length];
        Buffer.BlockCopy(preamble, 0, bytes, 0, preamble.Length);
        Buffer.BlockCopy(body, 0, bytes, preamble.Length, body.Length);
        return File(bytes, "text/csv", fileName);
    }

    private static string Csv(string? value)
    {
        value ??= "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }
}
