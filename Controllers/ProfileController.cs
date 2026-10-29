using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GrievancePortal.Data;
using GrievancePortal.Models;
using GrievancePortal.Models.ViewModels;

namespace GrievancePortal.Controllers;

[Authorize(Roles = Roles.Applicant)]
public class ProfileController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var userId = _userManager.GetUserId(User)!;
        var profile = await _db.EntrepreneurProfiles.FirstOrDefaultAsync(p => p.UserId == userId);

        var model = profile is null
            ? new ProfileFormModel { HasExistingProfile = false }
            : new ProfileFormModel
            {
                HasExistingProfile = true,
                BusinessName = profile.BusinessName,
                OwnerName = profile.OwnerName,
                Address = profile.Address,
                District = profile.District,
                Pincode = profile.Pincode,
                Phone = profile.Phone,
                UdyamNumber = profile.UdyamNumber,
                BusinessType = profile.BusinessType
            };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(ProfileFormModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var userId = _userManager.GetUserId(User)!;
        var profile = await _db.EntrepreneurProfiles.FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile is null)
        {
            profile = new EntrepreneurProfile
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };
            _db.EntrepreneurProfiles.Add(profile);
        }

        profile.BusinessName = model.BusinessName.Trim();
        profile.OwnerName = model.OwnerName.Trim();
        profile.Address = model.Address.Trim();
        profile.District = model.District.Trim();
        profile.Pincode = model.Pincode.Trim();
        profile.Phone = model.Phone.Trim();
        profile.UdyamNumber = model.UdyamNumber?.Trim();
        profile.BusinessType = model.BusinessType?.Trim();
        profile.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        TempData["Success"] = "Your business profile has been saved.";
        return RedirectToAction(nameof(Edit));
    }
}
