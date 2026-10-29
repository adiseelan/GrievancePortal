using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace GrievancePortal.Services;

/// <summary>
/// Stores uploads under a content-root folder that is NOT served statically
/// (outside wwwroot). Files are streamed back only through an authorized action.
/// </summary>
public class FileStorageService : IFileStorageService
{
    private readonly UploadSettings _settings;
    private readonly string _rootPath;

    public FileStorageService(IOptions<UploadSettings> settings, IWebHostEnvironment env)
    {
        _settings = settings.Value;

        // Resolve the storage folder relative to the content root (e.g. App_Data/uploads).
        _rootPath = Path.IsPathRooted(_settings.StorageFolder)
            ? _settings.StorageFolder
            : Path.Combine(env.ContentRootPath, _settings.StorageFolder);

        Directory.CreateDirectory(_rootPath);
    }

    public async Task<StoredFile> SaveAsync(IFormFile file, string subFolder, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            throw new FileValidationException("No file was uploaded.");

        if (file.Length > _settings.MaxFileSizeBytes)
            throw new FileValidationException($"File is too large. Maximum allowed size is {_settings.MaxFileSizeMB} MB.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_settings.AllowedExtensions.Contains(ext))
            throw new FileValidationException($"Only these file types are allowed: {string.Join(", ", _settings.AllowedExtensions)}.");

        // Lightweight content sniffing: a PDF must start with "%PDF-".
        if (ext == ".pdf" && !await LooksLikePdfAsync(file, ct))
            throw new FileValidationException("The uploaded file does not appear to be a valid PDF.");

        var safeSub = SanitizeSegment(subFolder);
        var targetDir = Path.Combine(_rootPath, safeSub);
        Directory.CreateDirectory(targetDir);

        var storedName = $"{Guid.NewGuid():N}{ext}";
        var absolutePath = Path.Combine(targetDir, storedName);

        await using (var stream = new FileStream(absolutePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await file.CopyToAsync(stream, ct);
        }

        // Store a relative path so the database is portable across machines.
        var relativePath = Path.Combine(safeSub, storedName).Replace('\\', '/');
        return new StoredFile(relativePath, Path.GetFileName(file.FileName));
    }

    public (Stream Stream, string ContentType)? OpenRead(string storedPath)
    {
        var absolute = ResolveInsideRoot(storedPath);
        if (absolute is null || !File.Exists(absolute))
            return null;

        var contentType = Path.GetExtension(absolute).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };

        Stream stream = new FileStream(absolute, FileMode.Open, FileAccess.Read, FileShare.Read);
        return (stream, contentType);
    }

    public void Delete(string? storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath)) return;
        var absolute = ResolveInsideRoot(storedPath);
        if (absolute is not null && File.Exists(absolute))
        {
            File.Delete(absolute);
        }
    }

    private static async Task<bool> LooksLikePdfAsync(IFormFile file, CancellationToken ct)
    {
        await using var s = file.OpenReadStream();
        var header = new byte[5];
        var read = await s.ReadAsync(header.AsMemory(0, 5), ct);
        return read == 5 && header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46 && header[4] == 0x2D;
    }

    private static string SanitizeSegment(string segment)
    {
        var cleaned = new string(segment.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_').ToArray());
        return string.IsNullOrEmpty(cleaned) ? "misc" : cleaned;
    }

    /// <summary>Resolves a stored relative path to an absolute path and guards against traversal.</summary>
    private string? ResolveInsideRoot(string storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath)) return null;

        var combined = Path.GetFullPath(Path.Combine(_rootPath, storedPath));
        var rootFull = Path.GetFullPath(_rootPath);

        // Ensure the resolved path stays within the storage root.
        if (!combined.StartsWith(rootFull + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            && !combined.Equals(rootFull, StringComparison.Ordinal))
        {
            return null;
        }
        return combined;
    }
}
