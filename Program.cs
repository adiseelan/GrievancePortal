using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GrievancePortal.Data;
using GrievancePortal.Models;
using GrievancePortal.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Database provider selection.
//   DatabaseProvider = "Sqlite"   -> local development (zero setup).
//   DatabaseProvider = "Postgres" -> production (Npgsql).
// Switching providers is a single configuration change (appsettings / env var).
// ---------------------------------------------------------------------------
var provider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "Sqlite";
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? "Data Source=grievanceportal.db";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (IsPostgres(provider))
    {
        options.UseNpgsql(connectionString);
    }
    else
    {
        options.UseSqlite(connectionString);
    }
});

static bool IsPostgres(string p) =>
    p.Equals("Postgres", StringComparison.OrdinalIgnoreCase) ||
    p.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) ||
    p.Equals("Npgsql", StringComparison.OrdinalIgnoreCase);

// ---------------------------------------------------------------------------
// ASP.NET Core Identity (with roles). Uses Identity's default password hashing.
// ---------------------------------------------------------------------------
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireDigit = true;
        options.User.RequireUniqueEmail = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Re-validate the signed-in user against the database on every request. If the
// user no longer exists (e.g. the development database was reset while an old
// auth cookie is still in the browser), the cookie is rejected and the user is
// signed out automatically — instead of reaching the database and failing with a
// foreign-key error when saving related data.
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.Zero;
});

// Authorization policies (also usable directly as [Authorize(Roles = "...")]).
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole(Roles.Admin));
    options.AddPolicy("OfficerOnly", p => p.RequireRole(Roles.Officer));
    options.AddPolicy("StaffOnly", p => p.RequireRole(Roles.Admin, Roles.Officer));
});

// ---------------------------------------------------------------------------
// Application services.
// ---------------------------------------------------------------------------
builder.Services.Configure<UploadSettings>(builder.Configuration.GetSection("Uploads"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));

builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IAuditService, AuditService>();

// Email is OFF by default. Swap in the SMTP sender only when enabled in config.
if (builder.Configuration.GetValue<bool>("Email:Enabled"))
{
    builder.Services.AddScoped<IApplicationEmailSender, SmtpEmailSender>();
}
else
{
    builder.Services.AddScoped<IApplicationEmailSender, NoOpEmailSender>();
}

// MVC + global anti-forgery enforcement on every unsafe (POST/PUT/DELETE) action.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

// Running behind Nginx: trust X-Forwarded-For / X-Forwarded-Proto from the proxy.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Single-host reverse proxy on the same machine; trust the local proxy.
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Friendly pages for 404 / 403 / 500 etc.
app.UseStatusCodePagesWithReExecute("/Home/Error", "?code={0}");

// NOTE: HTTPS redirection is intentionally NOT enabled here. In production,
// Nginx terminates TLS and redirects http->https, then proxies to Kestrel over
// plain HTTP on localhost. Enabling UseHttpsRedirection would cause a loop.
    app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ---------------------------------------------------------------------------
// Apply migrations and seed roles, admin user and sample schemes on startup.
// ---------------------------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    await SeedData.InitializeAsync(scope.ServiceProvider);
}

app.Run();
