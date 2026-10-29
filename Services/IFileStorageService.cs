using Microsoft.AspNetCore.Http;

namespace GrievancePortal.Services;

public record StoredFile(string StoredPath, string OriginalName);

public interface IFileStorageService
{
    /// <summary>Validates and saves an uploaded file to the protected storage folder.</summary>
    /// <returns>The stored file, or throws <see cref="FileValidationException"/> on invalid input.</returns>
    Task<StoredFile> SaveAsync(IFormFile file, string subFolder, CancellationToken ct = default);

    /// <summary>Opens a previously stored file for reading. Returns null if it no longer exists.</summary>
    (Stream Stream, string ContentType)? OpenRead(string storedPath);

    /// <summary>Deletes a stored file if present.</summary>
    void Delete(string? storedPath);
}

/// <summary>Thrown when an uploaded file fails validation (type / size).</summary>
public class FileValidationException : Exception
{
    public FileValidationException(string message) : base(message) { }
}
