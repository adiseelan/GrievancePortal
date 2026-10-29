using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using GrievancePortal.Models;

namespace GrievancePortal.Data;

/// <summary>
/// Applies pending migrations and seeds roles, the admin user and sample schemes.
/// Runs once on application startup and is idempotent.
/// </summary>
public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("SeedData");

        // Apply any pending EF Core migrations (creates the database on first run).
        await db.Database.MigrateAsync();

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var config = services.GetRequiredService<IConfiguration>();

        // 1) Roles
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation("Created role {Role}", role);
            }
        }

        // 2) Admin user (credentials from configuration / environment variables)
        var adminEmail = config["SeedAdmin:Email"];
        var adminPassword = config["SeedAdmin:Password"];
        var adminName = config["SeedAdmin:FullName"] ?? "Administrator";

        if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
        {
            await EnsureUserAsync(userManager, logger, adminEmail, adminPassword, adminName, Roles.Admin);
        }
        else
        {
            logger.LogWarning("SeedAdmin:Email / SeedAdmin:Password not configured; admin not seeded.");
        }

        // 2b) Demo Officer and Applicant accounts, so a fresh database is testable
        //     immediately (and gives ready logins for a demo). These are DEMO-ONLY
        //     convenience accounts — disable or change them for a real deployment by
        //     setting SeedDemoUsers to false in configuration.
        if (config.GetValue("SeedDemoUsers", true))
        {
            await EnsureUserAsync(userManager, logger,
                "officer@adiseelan.tech", "Officer@12345", "Demo Officer", Roles.Officer);
            await EnsureUserAsync(userManager, logger,
                "applicant@adiseelan.tech", "Applicant@12345", "Demo Applicant", Roles.Applicant);
        }

        // 3) Sample schemes (only when none exist)
        if (!await db.Schemes.AnyAsync())
        {
            db.Schemes.AddRange(
                new Scheme
                {
                    Name = "Capital Subsidy Scheme",
                    Department = "District Industries Centre",
                    Eligibility = "Registered micro and small manufacturing units with a valid Udyam registration and investment in plant & machinery within the prescribed ceiling.",
                    Description = "Back-ended capital subsidy on eligible investment in plant and machinery for new and expanding micro & small enterprises.",
                    IsActive = true
                },
                new Scheme
                {
                    Name = "Interest Subvention for Working Capital",
                    Department = "Industries & Commerce",
                    Eligibility = "Small enterprises with a working-capital loan sanctioned by a scheduled bank and up-to-date repayment record.",
                    Description = "Reimbursement of a portion of the interest paid on working-capital loans to ease the cost of credit for small enterprises.",
                    IsActive = true
                },
                new Scheme
                {
                    Name = "Technology Upgradation Grant",
                    Department = "Enterprise Development Institute",
                    Eligibility = "Existing units in operation for at least one year that are adopting energy-efficient or modern production technology.",
                    Description = "One-time grant supporting the adoption of improved technology, automation and quality-certification for small units.",
                    IsActive = true
                },
                new Scheme
                {
                    Name = "Women Entrepreneurship Support Scheme",
                    Department = "District Industries Centre",
                    Eligibility = "Enterprises where women hold at least 51% ownership, with a valid business registration.",
                    Description = "Enhanced subsidy support, hand-holding and priority processing for women-led micro and small enterprises.",
                    IsActive = true
                },
                new Scheme
                {
                    Name = "Stand-Up Margin Money Assistance",
                    Department = "Industries & Commerce",
                    Eligibility = "First-generation entrepreneurs setting up a new greenfield enterprise in the manufacturing or services sector.",
                    Description = "Margin-money assistance to help first-time entrepreneurs meet the promoter's contribution for a bank-financed project.",
                    IsActive = false
                }
            );

            await db.SaveChangesAsync();
            logger.LogInformation("Seeded sample schemes.");
        }
    }

    /// <summary>Creates a user with the given role if it doesn't already exist, and
    /// ensures the role is assigned. Idempotent.</summary>
    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        ILogger logger,
        string email,
        string password,
        string fullName,
        string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName
            };

            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, role);
                logger.LogInformation("Seeded {Role} user {Email}", role, email);
            }
            else
            {
                logger.LogError("Failed to seed {Role} user {Email}: {Errors}",
                    role, email, string.Join("; ", result.Errors.Select(e => e.Description)));
            }
        }
        else if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }
    }
}
