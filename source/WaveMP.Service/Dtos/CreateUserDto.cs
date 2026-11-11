using System.ComponentModel.DataAnnotations;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Creates a user (profile).
/// </summary>
public sealed class CreateUserDto
{
    /// <summary>
    /// Name of the profile, unique without regard to case.
    /// </summary>
    [Required]
    [StringLength(50)]
    public required string Name { get; init; }

    /// <summary>
    /// Initials, 1 to 3 characters; derived from the name if missing or empty.
    /// </summary>
    [StringLength(16)]
    public string? Initials { get; init; }

    /// <summary>
    /// Color as #rrggbb; the least used color of the palette if missing.
    /// </summary>
    [RegularExpression("^#[0-9a-fA-F]{6}$")]
    public string? Color { get; init; }

    /// <summary>
    /// Role; User if missing.
    /// </summary>
    public RoleDto? Role { get; init; }
}
