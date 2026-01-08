// Services/ITeamService.cs
using GameJam.Models.DTOs;
using GameJam.Models.Entities;

namespace GameJam.Services;

public interface ITeamService
{
    Task<(Team? team, List<string> errors)> RegisterTeamAsync(
        TeamRegistrationRequest request,
        string zipFileName,
        string originalZipFileName,
        long zipFileSize,
        CancellationToken cancellationToken = default);

    Task<string> GenerateUniqueFolderNameAsync(
        string teamName,
        CancellationToken cancellationToken = default);

    Task<List<string>> ValidateRegistrationAsync(
        TeamRegistrationRequest request,
        CancellationToken cancellationToken = default);
}