// Models/TeamRegistration.cs
using System.ComponentModel.DataAnnotations;

namespace SBUGameJam.Models;

public class TeamMember
{
    [Required(ErrorMessage = "First name is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required")]
    [Phone(ErrorMessage = "Invalid phone number format")]
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
    public List<TeamMember> Members { get; set; } = new();
}

