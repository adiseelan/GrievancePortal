using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using GrievancePortal.Models;

namespace GrievancePortal.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Scheme> Schemes => Set<Scheme>();
    public DbSet<EntrepreneurProfile> EntrepreneurProfiles => Set<EntrepreneurProfile>();
    public DbSet<SchemeApplication> SchemeApplications => Set<SchemeApplication>();
    public DbSet<Grievance> Grievances => Set<Grievance>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<EntrepreneurProfile>(e =>
        {
            e.HasIndex(p => p.UserId).IsUnique();
            e.HasOne(p => p.User)
                .WithOne()
                .HasForeignKey<EntrepreneurProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SchemeApplication>(e =>
        {
            e.HasIndex(a => a.ApplicationNumber).IsUnique();
            e.HasIndex(a => a.SchemeId);
            e.HasIndex(a => a.ApplicantId);

            e.HasOne(a => a.Scheme)
                .WithMany(s => s.Applications)
                .HasForeignKey(a => a.SchemeId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(a => a.Applicant)
                .WithMany()
                .HasForeignKey(a => a.ApplicantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Grievance>(e =>
        {
            e.HasIndex(g => g.TicketNumber).IsUnique();
            e.HasIndex(g => g.ApplicantId);
            e.HasIndex(g => g.AssignedOfficerId);

            e.HasOne(g => g.Applicant)
                .WithMany()
                .HasForeignKey(g => g.ApplicantId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(g => g.AssignedOfficer)
                .WithMany()
                .HasForeignKey(g => g.AssignedOfficerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AuditLog>(e =>
        {
            e.HasIndex(a => a.PerformedById);
            e.HasOne(a => a.PerformedBy)
                .WithMany()
                .HasForeignKey(a => a.PerformedById)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
