namespace GrievancePortal.Models;

/// <summary>Canonical role names used throughout the application.</summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Officer = "Officer";
    public const string Applicant = "Applicant";

    public static readonly string[] All = { Admin, Officer, Applicant };
}
