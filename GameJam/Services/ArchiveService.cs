// Services/ArchiveService.cs
using SharpCompress.Archives;

namespace GameJam.Services;

public class ArchiveService : IArchiveService
{
    private readonly ILogger<ArchiveService> _logger;
    private static readonly string[] SupportedExtensions = { ".zip", ".rar" };

    public ArchiveService(ILogger<ArchiveService> logger)
    {
        _logger = logger;
    }

    public bool IsArchiveFile(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return SupportedExtensions.Contains(extension);
    }

    public string GetArchiveExtension(string fileName)
    {
        return Path.GetExtension(fileName).ToLowerInvariant();
    }

    public async Task<(bool isValid, string? errorMessage)> ValidateArchiveAsync(
        Stream fileStream,
        string fileName,
        string destinationFolder,
        CancellationToken cancellationToken = default)
    {
        try
        {
            fileStream.Position = 0;

            using var archive = ArchiveFactory.Open(fileStream);

            if (archive == null)
            {
                return (false, "فایل فشرده نامعتبر است");
            }

            // Check if archive is encrypted
            foreach (var entry in archive.Entries)
            {
                if (entry.IsEncrypted)
                {
                    return (false, "فایل‌های رمزگذاری شده مجاز نیستند");
                }

                // Security check: prevent path traversal
                if (!string.IsNullOrEmpty(entry.Key))
                {
                    var fullPath = Path.GetFullPath(Path.Combine(destinationFolder, entry.Key));
                    if (!fullPath.StartsWith(destinationFolder, StringComparison.OrdinalIgnoreCase))
                    {
                        return (false, "فایل فشرده شامل مسیرهای غیرمجاز است");
                    }
                }

                // Check for suspicious files
                if (!entry.IsDirectory)
                {
                    var entryExtension = Path.GetExtension(entry.Key).ToLowerInvariant();
                    if (IsSuspiciousExtension(entryExtension))
                    {
                        _logger.LogWarning("Suspicious file detected in archive: {FileName}", entry.Key);
                        return (false, $"فایل با پسوند غیرمجاز یافت شد: {entryExtension}");
                    }
                }
            }

            return (true, null);
        }
        catch (InvalidOperationException)
        {
            return (false, "فرمت فایل فشرده نامعتبر است");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating archive file: {FileName}", fileName);
            return (false, "خطا در اعتبارسنجی فایل فشرده");
        }
    }

    private static bool IsSuspiciousExtension(string extension)
    {
        // لیست پسوندهای خطرناک
        var dangerousExtensions = new[]
        {
            ".exe", ".dll", ".bat", ".cmd", ".com", ".msi", ".scr",
            ".vbs", ".js", ".jar", ".ps1", ".psm1", ".psd1"
        };

        return dangerousExtensions.Contains(extension);
    }
}