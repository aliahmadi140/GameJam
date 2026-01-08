// Models/Entities/Team.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SBUGameJam.Models;

namespace GameJam.Models.Entities;

[Table("Teams")]
public class Team
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string TeamName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string SanitizedFolderName { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? ZipFileName { get; set; }

    [MaxLength(255)]
    public string? OriginalZipFileName { get; set; }

    public long? ZipFileSize { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation property
    public virtual ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();
}