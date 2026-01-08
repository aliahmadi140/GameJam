// Models/DTOs/TeamRegistrationDto.cs
using System.ComponentModel.DataAnnotations;

namespace GameJam.Models.DTOs;

public class TeamMemberDto
{
    [Required(ErrorMessage = "First name is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required")]
    [RegularExpression(@"^09\d{9}$", ErrorMessage = "Phone number must be in format: 09XXXXXXXXX")]
    public string PhoneNumber { get; set; } = string.Empty;
}

public class TeamRegistrationRequest
{
    [Required(ErrorMessage = "Team name is required")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Team name must be between 3 and 100 characters")]
    [RegularExpression(@"^[a-zA-Z0-9\-_\s\u0600-\u06FF]+$", ErrorMessage = "Team name contains invalid characters")]
    public string TeamName { get; set; } = string.Empty;

    [Required(ErrorMessage = "At least one team member is required")]
    [MinLength(1, ErrorMessage = "At least one team member is required")]
    [MaxLength(4, ErrorMessage = "Maximum 4 team members allowed")]
    public List<TeamMemberDto> Members { get; set; } = new();
}

public class TeamRegistrationResponse
{
    public int TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public string FolderName { get; set; } = string.Empty;
    public int MemberCount { get; set; }
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();
}