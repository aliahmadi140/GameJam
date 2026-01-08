// Controllers/RegistrationController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using GameJam.Models.DTOs;
using GameJam.Repositories;
using GameJam.Services;
using GameJam.Attributes;
using GameJam.Helpers;
using System.Text.Json;
using System.Text;

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
    private const long MaxFileSize = 3L * 1024 * 1024 * 1024; // 3GB

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

        _logger.LogInformation("RegistrationController initialized");
    }

    [HttpPost("submit")]
    [DisableFormValueModelBinding] // ✅ غیرفعال کردن model binding
    [RequestSizeLimit(3L * 1024 * 1024 * 1024)]
    [RequestFormLimits(
        MultipartBodyLengthLimit = 3L * 1024 * 1024 * 1024,
        ValueLengthLimit = int.MaxValue,
        MultipartHeadersLengthLimit = int.MaxValue)]
    public async Task<ActionResult<ApiResponse<TeamRegistrationResponse>>> SubmitRegistration(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("=== SubmitRegistration Action Started ===");

        var response = new ApiResponse<TeamRegistrationResponse>();
        var errors = new List<string>();

        try
        {
            // بررسی Content-Type
            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                _logger.LogWarning("Invalid content type: {ContentType}", Request.ContentType);
                response.Success = false;
                response.Message = "Invalid content type";
                response.Errors.Add("Request must be multipart/form-data");
                return BadRequest(response);
            }

            // خواندن boundary
            var boundary = MultipartRequestHelper.GetBoundary(
                MediaTypeHeaderValue.Parse(Request.ContentType),
                70); // default boundary length limit

            var reader = new MultipartReader(boundary, Request.Body);

            string? teamDataJson = null;
            string? archiveFilePath = null;
            string? originalFileName = null;
            long fileSize = 0;

            MultipartSection? section;

            _logger.LogInformation("Starting to read multipart sections...");

            // خواندن هر section
            while ((section = await reader.ReadNextSectionAsync(cancellationToken)) != null)
            {
                var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(
                    section.ContentDisposition,
                    out var contentDisposition);

                if (!hasContentDispositionHeader)
                {
                    continue;
                }

                // اگر فیلد معمولی است (teamData)
                if (MultipartRequestHelper.HasFormDataContentDisposition(contentDisposition))
                {
                    var key = HeaderUtilities.RemoveQuotes(contentDisposition!.Name).Value;

                    _logger.LogInformation("Reading form field: {Key}", key);

                    using var streamReader = new StreamReader(section.Body);
                    var value = await streamReader.ReadToEndAsync(cancellationToken);

                    if (key == "teamData")
                    {
                        teamDataJson = value;
                        _logger.LogInformation("TeamData received: {Length} characters", value.Length);
                    }
                }
                // اگر فایل است (archiveFile)
                else if (MultipartRequestHelper.HasFileContentDisposition(contentDisposition))
                {
                    var fileNameSegment = contentDisposition!.FileName.HasValue
    ? contentDisposition.FileName
    : contentDisposition.FileNameStar;

                    originalFileName = HeaderUtilities.RemoveQuotes(fileNameSegment).Value;


                    _logger.LogInformation("Reading file: {FileName}", originalFileName);

                    if (string.IsNullOrEmpty(originalFileName))
                    {
                        errors.Add("نام فایل نامعتبر است");
                        continue;
                    }

                    // بررسی پسوند فایل
                    if (!_archiveService.IsArchiveFile(originalFileName))
                    {
                        errors.Add("فقط فایل‌های ZIP و RAR مجاز هستند");
                        continue;
                    }

                    // ایجاد فایل موقت برای ذخیره
                    var tempFileName = $"temp_{Guid.NewGuid()}{Path.GetExtension(originalFileName)}";
                    var tempFilePath = Path.Combine(_uploadsPath, tempFileName);

                    _logger.LogInformation("Saving file to: {TempPath}", tempFilePath);

                    // ذخیره فایل به صورت stream (برای فایل‌های بزرگ)
                    await using (var targetStream = System.IO.File.Create(tempFilePath))
                    {
                        await section.Body.CopyToAsync(targetStream, cancellationToken);
                        fileSize = targetStream.Length;
                    }

                    _logger.LogInformation("File saved successfully. Size: {Size} bytes", fileSize);

                    // بررسی حجم فایل
                    if (fileSize > MaxFileSize)
                    {
                        System.IO.File.Delete(tempFilePath);
                        errors.Add($"حجم فایل باید کمتر از {MaxFileSize / (1024 * 1024 * 1024)} گیگابایت باشد");
                        continue;
                    }

                    archiveFilePath = tempFilePath;
                }
            }

            _logger.LogInformation("Finished reading multipart sections");

            // اعتبارسنجی داده‌های دریافتی
            if (string.IsNullOrEmpty(teamDataJson))
            {
                errors.Add("اطلاعات تیم ارسال نشده است");
            }

            if (string.IsNullOrEmpty(archiveFilePath))
            {
                errors.Add("فایل فشرده ارسال نشده است");
            }

            if (errors.Count > 0)
            {
                // پاک کردن فایل موقت در صورت خطا
                if (!string.IsNullOrEmpty(archiveFilePath) && System.IO.File.Exists(archiveFilePath))
                {
                    System.IO.File.Delete(archiveFilePath);
                }

                response.Success = false;
                response.Message = "خطا در اعتبارسنجی";
                response.Errors = errors;
                return BadRequest(response);
            }

            // Deserialize team data
            TeamRegistrationRequest? request;
            try
            {
                request = JsonSerializer.Deserialize<TeamRegistrationRequest>(
                    teamDataJson!,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                _logger.LogInformation("Team data deserialized: {TeamName}", request?.TeamName);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Invalid JSON data");

                if (!string.IsNullOrEmpty(archiveFilePath) && System.IO.File.Exists(archiveFilePath))
                {
                    System.IO.File.Delete(archiveFilePath);
                }

                response.Success = false;
                response.Message = "Invalid data format";
                response.Errors.Add("Could not parse team data");
                return BadRequest(response);
            }

            if (request == null)
            {
                if (!string.IsNullOrEmpty(archiveFilePath) && System.IO.File.Exists(archiveFilePath))
                {
                    System.IO.File.Delete(archiveFilePath);
                }

                response.Success = false;
                response.Message = "Invalid request";
                response.Errors.Add("Team data is required");
                return BadRequest(response);
            }

            // اعتبارسنجی داده‌های تیم
            errors = ValidateTeamData(request);
            if (errors.Count > 0)
            {
                if (!string.IsNullOrEmpty(archiveFilePath) && System.IO.File.Exists(archiveFilePath))
                {
                    System.IO.File.Delete(archiveFilePath);
                }

                response.Success = false;
                response.Message = "خطا در اعتبارسنجی";
                response.Errors = errors;
                return BadRequest(response);
            }

            // ایجاد پوشه تیم
            var folderName = await _teamService.GenerateUniqueFolderNameAsync(
                request.TeamName, cancellationToken);

            var teamFolder = Path.Combine(_uploadsPath, folderName);
            Directory.CreateDirectory(teamFolder);

            _logger.LogInformation("Team folder created: {FolderName}", folderName);

            // انتقال فایل به پوشه نهایی
            var archiveExtension = _archiveService.GetArchiveExtension(originalFileName!);
            var finalArchiveFileName = $"{folderName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}{archiveExtension}";
            var finalArchiveFilePath = Path.Combine(teamFolder, finalArchiveFileName);

            System.IO.File.Move(archiveFilePath!, finalArchiveFilePath);

            _logger.LogInformation("File moved to final location: {FinalPath}", finalArchiveFilePath);

            // اعتبارسنجی محتوای فایل فشرده
            using (var fileStream = new FileStream(finalArchiveFilePath, FileMode.Open, FileAccess.Read))
            {
                var (isValid, errorMessage) = await _archiveService.ValidateArchiveAsync(
                    fileStream,
                    originalFileName!,
                    teamFolder,
                    cancellationToken);

                if (!isValid)
                {
                    System.IO.File.Delete(finalArchiveFilePath);
                    Directory.Delete(teamFolder, true);

                    response.Success = false;
                    response.Message = "فایل فشرده نامعتبر است";
                    response.Errors.Add(errorMessage ?? "خطای نامشخص");
                    return BadRequest(response);
                }
            }

            _logger.LogInformation("Archive validation passed");

            // ذخیره در دیتابیس
            var (team, dbErrors) = await _teamService.RegisterTeamAsync(
                request,
                finalArchiveFileName,
                originalFileName!,
                fileSize,
                cancellationToken);

            if (team == null)
            {
                if (System.IO.File.Exists(finalArchiveFilePath))
                    System.IO.File.Delete(finalArchiveFilePath);
                if (Directory.Exists(teamFolder))
                    Directory.Delete(teamFolder, true);

                response.Success = false;
                response.Message = "خطا در ثبت‌نام";
                response.Errors = dbErrors;
                return BadRequest(response);
            }

            _logger.LogInformation("Team registered successfully: {TeamId}", team.Id);

            // ذخیره اطلاعات به صورت JSON
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

            _logger.LogInformation("Registration completed successfully");

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

    private List<string> ValidateTeamData(TeamRegistrationRequest request)
    {
        var errors = new List<string>();

        // Team name validation
        if (string.IsNullOrWhiteSpace(request.TeamName))
        {
            errors.Add("نام تیم الزامی است");
        }
        else if (request.TeamName.Length < 3 || request.TeamName.Length > 100)
        {
            errors.Add("نام تیم باید بین ۳ تا ۱۰۰ کاراکتر باشد");
        }

        // Members validation
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
                else if (!System.Text.RegularExpressions.Regex.IsMatch(member.PhoneNumber, @"^09\d{9}$"))
                    errors.Add($"عضو {memberNum}: فرمت شماره تلفن نامعتبر است");
            }
        }

        return errors;
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