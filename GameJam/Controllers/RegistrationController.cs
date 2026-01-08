// Controllers/RegistrationController.cs
using Microsoft.AspNetCore.Mvc;
using GameJam.Models.DTOs;
using GameJam.Repositories;
using GameJam.Services;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GameJam.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RegistrationController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<RegistrationController> _logger;
    private readonly ITeamService _teamService;
    private readonly IArchiveService _archiveService;
    private readonly string _uploadsPath;
    private const long MaxFileSize = 100 * 1024 * 1024; // 100MB
    private static readonly string[] AllowedExtensions = { ".zip", ".rar" };

    public RegistrationController(
        IWebHostEnvironment environment,
        ILogger<RegistrationController> logger,
        ITeamService teamService,
        IArchiveService archiveService)
    {
        _environment = environment;
        _logger = logger;
        _teamService = teamService;
        _archiveService = archiveService;
        _uploadsPath = Path.Combine(_environment.ContentRootPath, "Uploads");

        if (!Directory.Exists(_uploadsPath))
        {
            Directory.CreateDirectory(_uploadsPath);
        }
    }

    [HttpPost("submit")]
    [RequestSizeLimit(MaxFileSize)]
    public async Task<ActionResult<ApiResponse<TeamRegistrationResponse>>> SubmitRegistration(
        [FromForm] string teamData,
        [FromForm] IFormFile? archiveFile,
        CancellationToken cancellationToken)
    {
        var response = new ApiResponse<TeamRegistrationResponse>();
        var errors = new List<string>();

        try
        {
            // Deserialize team data
            TeamRegistrationRequest? request;
            try
            {
                request = JsonSerializer.Deserialize<TeamRegistrationRequest>(teamData, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Invalid JSON data received");
                response.Success = false;
                response.Message = "Invalid data format";
                response.Errors.Add("Could not parse team data");
                return BadRequest(response);
            }

            if (request == null)
            {
                response.Success = false;
                response.Message = "Invalid request";
                response.Errors.Add("Team data is required");
                return BadRequest(response);
            }

            // Validate team name
            if (string.IsNullOrWhiteSpace(request.TeamName))
            {
                errors.Add("نام تیم الزامی است");
            }
            else if (request.TeamName.Length < 3 || request.TeamName.Length > 100)
            {
                errors.Add("نام تیم باید بین ۳ تا ۱۰۰ کاراکتر باشد");
            }
            else if (!Regex.IsMatch(request.TeamName, @"^[a-zA-Z0-9\-_\s\u0600-\u06FF]+$"))
            {
                errors.Add("نام تیم شامل کاراکترهای غیرمجاز است");
            }

            // Validate members
            if (request.Members == null || request.Members.Count == 0)
            {
                errors.Add("حداقل یک عضو برای تیم الزامی است");
            }
            else if (request.Members.Count > 4)
            {
                errors.Add("حداکثر ۴ عضو برای هر تیم مجاز است");
            }
            else
            {
                for (int i = 0; i < request.Members.Count; i++)
                {
                    var member = request.Members[i];
                    var memberNum = i + 1;

                    if (string.IsNullOrWhiteSpace(member.FirstName))
                        errors.Add($"عضو {memberNum}: نام الزامی است");
                    else if (member.FirstName.Length < 2 || member.FirstName.Length > 50)
                        errors.Add($"عضو {memberNum}: نام باید بین ۲ تا ۵۰ کاراکتر باشد");

                    if (string.IsNullOrWhiteSpace(member.LastName))
                        errors.Add($"عضو {memberNum}: نام خانوادگی الزامی است");
                    else if (member.LastName.Length < 2 || member.LastName.Length > 50)
                        errors.Add($"عضو {memberNum}: نام خانوادگی باید بین ۲ تا ۵۰ کاراکتر باشد");

                    if (string.IsNullOrWhiteSpace(member.PhoneNumber))
                        errors.Add($"عضو {memberNum}: شماره تلفن الزامی است");
                    else if (!Regex.IsMatch(member.PhoneNumber, @"^09\d{9}$"))
                        errors.Add($"عضو {memberNum}: فرمت شماره تلفن نامعتبر است");
                }
            }

            // Validate archive file
            if (archiveFile == null || archiveFile.Length == 0)
            {
                errors.Add("آپلود فایل فشرده (ZIP یا RAR) الزامی است");
            }
            else
            {
                var extension = Path.GetExtension(archiveFile.FileName).ToLowerInvariant();

                if (!AllowedExtensions.Contains(extension))
                {
                    errors.Add("فقط فایل‌های ZIP و RAR مجاز هستند");
                }

                if (archiveFile.Length > MaxFileSize)
                {
                    errors.Add($"حجم فایل باید کمتر از {MaxFileSize / (1024 * 1024)} مگابایت باشد");
                }

                if (!_archiveService.IsArchiveFile(archiveFile.FileName))
                {
                    errors.Add("فرمت فایل پشتیبانی نمی‌شود");
                }
            }

            if (errors.Count > 0)
            {
                response.Success = false;
                response.Message = "خطا در اعتبارسنجی";
                response.Errors = errors;
                return BadRequest(response);
            }

            // Generate unique folder name
            var folderName = await _teamService.GenerateUniqueFolderNameAsync(
                request!.TeamName, cancellationToken);

            var teamFolder = Path.Combine(_uploadsPath, folderName);
            Directory.CreateDirectory(teamFolder);

            // Save archive file
            var archiveExtension = _archiveService.GetArchiveExtension(archiveFile!.FileName);
            var archiveFileName = $"{folderName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}{archiveExtension}";
            var archiveFilePath = Path.Combine(teamFolder, archiveFileName);

            await using (var fileStream = new FileStream(archiveFilePath, FileMode.Create))
            {
                await archiveFile.CopyToAsync(fileStream, cancellationToken);
            }

            // Validate archive contents (security check)
            using (var fileStream = new FileStream(archiveFilePath, FileMode.Open, FileAccess.Read))
            {
                var (isValid, errorMessage) = await _archiveService.ValidateArchiveAsync(
                    fileStream,
                    archiveFile.FileName,
                    teamFolder,
                    cancellationToken);

                if (!isValid)
                {
                    System.IO.File.Delete(archiveFilePath);
                    Directory.Delete(teamFolder, true);

                    response.Success = false;
                    response.Message = "فایل فشرده نامعتبر است";
                    response.Errors.Add(errorMessage ?? "خطای نامشخص");
                    return BadRequest(response);
                }
            }

            // Save to database
            var (team, dbErrors) = await _teamService.RegisterTeamAsync(
                request,
                archiveFileName,
                archiveFile.FileName,
                archiveFile.Length,
                cancellationToken);

            if (team == null)
            {
                // Cleanup uploaded files on database error
                if (System.IO.File.Exists(archiveFilePath))
                    System.IO.File.Delete(archiveFilePath);
                if (Directory.Exists(teamFolder))
                    Directory.Delete(teamFolder, true);

                response.Success = false;
                response.Message = "خطا در ثبت‌نام";
                response.Errors = dbErrors;
                return BadRequest(response);
            }

            // Save team info as JSON (backup)
            var teamInfoPath = Path.Combine(teamFolder, "team_info.json");
            var teamInfo = new
            {
                TeamId = team.Id,
                team.TeamName,
                team.SanitizedFolderName,
                Members = team.Members.Select(m => new
                {
                    m.FirstName,
                    m.LastName,
                    m.PhoneNumber,
                    m.DisplayOrder
                }),
                SubmittedAt = team.CreatedAt,
                ArchiveFileName = team.ZipFileName,
                OriginalArchiveFileName = team.OriginalZipFileName,
                ArchiveFileSize = team.ZipFileSize,
                ArchiveType = archiveExtension
            };

            await System.IO.File.WriteAllTextAsync(
                teamInfoPath,
                JsonSerializer.Serialize(teamInfo, new JsonSerializerOptions { WriteIndented = true }),
                cancellationToken);

            response.Success = true;
            response.Message = "ثبت‌نام با موفقیت انجام شد!";
            response.Data = new TeamRegistrationResponse
            {
                TeamId = team.Id,
                TeamName = team.TeamName,
                FolderName = team.SanitizedFolderName,
                MemberCount = team.Members.Count
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing registration");
            response.Success = false;
            response.Message = "خطای سرور";
            response.Errors.Add("خطای داخلی سرور رخ داده است");
            return StatusCode(500, response);
        }
    }

    [HttpGet("teams")]
    public async Task<ActionResult<ApiResponse<List<object>>>> GetAllTeams(
        [FromServices] ITeamRepository teamRepository,
        CancellationToken cancellationToken)
    {
        var teams = await teamRepository.GetAllAsync(cancellationToken);

        var result = teams.Select(t => new
        {
            t.Id,
            t.TeamName,
            t.SanitizedFolderName,
            MemberCount = t.Members.Count,
            Members = t.Members.Select(m => new
            {
                m.FirstName,
                m.LastName,
                m.PhoneNumber
            }),
            t.CreatedAt,
            ArchiveFileName = t.ZipFileName,
            ArchiveType = Path.GetExtension(t.ZipFileName ?? "")
        }).ToList();

        return Ok(new ApiResponse<List<object>>
        {
            Success = true,
            Message = "Teams retrieved successfully",
            Data = result.Cast<object>().ToList()
        });
    }
}