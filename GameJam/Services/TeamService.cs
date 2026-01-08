// Services/TeamService.cs
using GameJam.Models.DTOs;
using GameJam.Models.Entities;
using GameJam.Repositories;
using System.Text.RegularExpressions;

namespace GameJam.Services;

public class TeamService : ITeamService
{
    private readonly ITeamRepository _teamRepository;
    private readonly ILogger<TeamService> _logger;

    public TeamService(ITeamRepository teamRepository, ILogger<TeamService> logger)
    {
        _teamRepository = teamRepository;
        _logger = logger;
    }

    public async Task<List<string>> ValidateRegistrationAsync(
        TeamRegistrationRequest request,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        // Check for duplicate phone numbers within the request
        var phoneNumbers = request.Members.Select(m => m.PhoneNumber).ToList();
        var duplicatePhones = phoneNumbers
            .GroupBy(p => p)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicatePhones.Any())
        {
            errors.Add($"شماره تلفن تکراری در اعضای تیم: {string.Join(", ", duplicatePhones)}");
        }

        // Check if any phone number is already registered
        foreach (var member in request.Members)
        {
            if (await _teamRepository.ExistsByPhoneNumberAsync(member.PhoneNumber, cancellationToken))
            {
                errors.Add($"شماره تلفن {member.PhoneNumber} قبلاً ثبت شده است");
            }
        }

        return errors;
    }

    public async Task<string> GenerateUniqueFolderNameAsync(
        string teamName,
        CancellationToken cancellationToken = default)
    {
        var sanitized = SanitizeFolderName(teamName);
        var originalName = sanitized;
        var counter = 1;

        while (await _teamRepository.ExistsByFolderNameAsync(sanitized, cancellationToken))
        {
            sanitized = $"{originalName}_{counter}";
            counter++;
        }

        return sanitized;
    }

    public async Task<(Team? team, List<string> errors)> RegisterTeamAsync(
        TeamRegistrationRequest request,
        string zipFileName,
        string originalZipFileName,
        long zipFileSize,
        CancellationToken cancellationToken = default)
    {
        var errors = await ValidateRegistrationAsync(request, cancellationToken);

        if (errors.Any())
        {
            return (null, errors);
        }

        var folderName = await GenerateUniqueFolderNameAsync(request.TeamName, cancellationToken);

        var team = new Team
        {
            TeamName = request.TeamName,
            SanitizedFolderName = folderName,
            ZipFileName = zipFileName,
            OriginalZipFileName = originalZipFileName,
            ZipFileSize = zipFileSize,
            CreatedAt = DateTime.UtcNow
        };

        // Add members
        for (int i = 0; i < request.Members.Count; i++)
        {
            var memberDto = request.Members[i];
            team.Members.Add(new TeamMember
            {
                FirstName = memberDto.FirstName,
                LastName = memberDto.LastName,
                PhoneNumber = memberDto.PhoneNumber,
                DisplayOrder = i + 1,
                CreatedAt = DateTime.UtcNow
            });
        }

        try
        {
            var createdTeam = await _teamRepository.CreateAsync(team, cancellationToken);
            _logger.LogInformation(
                "Team '{TeamName}' (ID: {TeamId}) registered successfully with {MemberCount} members",
                createdTeam.TeamName, createdTeam.Id, createdTeam.Members.Count);

            return (createdTeam, new List<string>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating team '{TeamName}'", request.TeamName);
            errors.Add("خطا در ذخیره اطلاعات تیم");
            return (null, errors);
        }
    }

    private static string SanitizeFolderName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Where(c => !invalidChars.Contains(c)).ToArray());

        sanitized = sanitized.Replace(' ', '_');
        sanitized = Regex.Replace(sanitized, @"_+", "_");
        sanitized = sanitized.Trim('_');

        if (string.IsNullOrWhiteSpace(sanitized))
        {
            sanitized = $"team_{Guid.NewGuid().ToString("N")[..8]}";
        }

        if (sanitized.Length > 50)
        {
            sanitized = sanitized[..50];
        }

        return sanitized;
    }
}