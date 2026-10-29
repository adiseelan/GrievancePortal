using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GrievancePortal.Data;
using GrievancePortal.Models.ViewModels;

namespace GrievancePortal.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;

    public HomeController(ApplicationDbContext db) => _db = db;

    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        // Show a few active schemes on the landing page.
        var schemes = await _db.Schemes
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.CreatedAt)
            .Take(3)
            .ToListAsync();

        return View(schemes);
    }

    [AllowAnonymous]
    public IActionResult Privacy() => View();

    [AllowAnonymous]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true, Duration = 0)]
    public IActionResult Error(int? code)
    {
        var vm = new ErrorViewModel
        {
            StatusCode = code,
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        };

        (vm.Title, vm.Message) = code switch
        {
            404 => ("Page not found", "The page you were looking for doesn't exist or may have been moved."),
            403 => ("Access denied", "You don't have permission to view this page."),
            401 => ("Sign in required", "Please sign in to continue."),
            400 => ("Bad request", "The request could not be understood by the server."),
            500 => ("Server error", "Something went wrong on our side. Please try again later."),
            _ => ("Something went wrong", "An unexpected error occurred while processing your request.")
        };

        return View(vm);
    }
}
