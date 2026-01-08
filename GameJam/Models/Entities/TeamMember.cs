// Models/Entities/TeamMember.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameJam.Models.Entities;

[Table("TeamMembers")]
public class TeamMember
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int TeamId { get; set; }

    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [MaxLength(11)]
    public string PhoneNumber { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey(nameof(TeamId))]
    public virtual Team Team { get; set; } = null!;
}