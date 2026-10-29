namespace GrievancePortal.Services;

/// <summary>Bound from the "Uploads" configuration section.</summary>
public class UploadSettings
{
    public int MaxFileSizeMB { get; set; } = 5;
    public string[] AllowedExtensions { get; set; } = new[] { ".pdf" };
    public string StorageFolder { get; set; } = "App_Data/uploads";

    public long MaxFileSizeBytes => (long)MaxFileSizeMB * 1024 * 1024;
}

/// <summary>Bound from the "Email" configuration section.</summary>
public class EmailSettings
{
    public bool Enabled { get; set; }
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = "no-reply@localhost";
    public string FromName { get; set; } = "Subsidy & Grievance Portal";
}
