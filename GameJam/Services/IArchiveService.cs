// Services/IArchiveService.cs
namespace GameJam.Services;

public interface IArchiveService
{
    Task<(bool isValid, string? errorMessage)> ValidateArchiveAsync(
        Stream fileStream,
        string fileName,
        string destinationFolder,
        CancellationToken cancellationToken = default);

    bool IsArchiveFile(string fileName);
    string GetArchiveExtension(string fileName);
}