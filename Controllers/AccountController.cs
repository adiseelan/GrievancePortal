using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using GrievancePortal.Models;
using GrievancePortal.Models.ViewModels;

namespace GrievancePortal.Controllers;

[Authorize]
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (_signInManager.IsSignedIn(User))
            return RedirectToAction("Index", "Home");

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _signInManager.PasswordSignInAsync(
            model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {Email} signed in.", model.Email);
            return RedirectToLocal(model.ReturnUrl);
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "This account is temporarily locked due to multiple failed attempts. Please try again later.");
            return View(model);
        }

        ModelState.AddModelError(string.Empty, "Invalid email or password.");
        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register(string? returnUrl = null)
    {
        if (_signInManager.IsSignedIn(User))
            return RedirectToAction("Index", "Home");

        return View(new RegisterViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            EmailConfirmed = true // no email-confirmation flow in this build
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            // Every self-registered user is an Applicant.
            await _userManager.AddToRoleAsync(user, Roles.Applicant);
            await _signInManager.SignInAsync(user, isPersistent: false);
            _logger.LogInformation("New applicant registered: {Email}", model.Email);
            return RedirectToLocal(model.ReturnUrl);
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User signed out.");
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }
}
