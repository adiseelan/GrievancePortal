using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GrievancePortal.Data;
using GrievancePortal.Models;
using GrievancePortal.Models.ViewModels;

namespace GrievancePortal.Controllers;

public class SchemesController : Controller
{
    private readonly ApplicationDbContext _db;

    public SchemesController(ApplicationDbContext db) => _db = db;

    // ---- Public: list active schemes ----
    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        var schemes = await _db.Schemes
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync();

        return View(schemes);
    }

    // ---- Public: scheme details (active only, unless staff) ----
    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var scheme = await _db.Schemes.FirstOrDefaultAsync(s => s.Id == id);
        if (scheme is null)
            return NotFound();

        var isStaff = User.IsInRole(Roles.Admin) || User.IsInRole(Roles.Officer);
        if (!scheme.IsActive && !isStaff)
            return NotFound();

        return View(scheme);
    }

    // ---- Admin: manage all schemes ----
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Manage()
    {
        var schemes = await _db.Schemes
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        return View(schemes);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpGet]
    public IActionResult Create() => View(new SchemeFormModel());

    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    public async Task<IActionResult> Create(SchemeFormModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var scheme = new Scheme
        {
            Name = model.Name.Trim(),
            Department = model.Department.Trim(),
            Eligibility = model.Eligibility.Trim(),
            Description = model.Description.Trim(),
            IsActive = model.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _db.Schemes.Add(scheme);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Scheme \"{scheme.Name}\" created.";
        return RedirectToAction(nameof(Manage));
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var scheme = await _db.Schemes.FindAsync(id);
        if (scheme is null)
            return NotFound();

        return View(SchemeFormModel.FromEntity(scheme));
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    public async Task<IActionResult> Edit(int id, SchemeFormModel model)
    {
        if (id != model.Id)
            return BadRequest();

        if (!ModelState.IsValid)
            return View(model);

        var scheme = await _db.Schemes.FindAsync(id);
        if (scheme is null)
            return NotFound();

        scheme.Name = model.Name.Trim();
        scheme.Department = model.Department.Trim();
        scheme.Eligibility = model.Eligibility.Trim();
        scheme.Description = model.Description.Trim();
        scheme.IsActive = model.IsActive;

        await _db.SaveChangesAsync();

        TempData["Success"] = $"Scheme \"{scheme.Name}\" updated.";
        return RedirectToAction(nameof(Manage));
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var scheme = await _db.Schemes.FindAsync(id);
        if (scheme is null)
            return NotFound();

        scheme.IsActive = !scheme.IsActive;
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Scheme \"{scheme.Name}\" is now {(scheme.IsActive ? "active" : "inactive")}.";
        return RedirectToAction(nameof(Manage));
    }
}
