using System.ComponentModel.DataAnnotations;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Changes a user (profile). Missing or null values stay unchanged.
/// </summary>
public sealed class UpdateUserDto
{
    /// <summary>
    /// New name; initials derived from the old name follow it.
    /// </summary>
    [StringLength(50, MinimumLength = 1)]
    public string? Name { get; init; }

    /// <summary>
    /// New initials, 1 to 3 characters; an empty string derives them from the name again.
    /// </summary>
    [StringLength(16)]
    public string? Initials { get; init; }

    /// <summary>
    /// New color as #rrggbb.
    /// </summary>
    [RegularExpression("^#[0-9a-fA-F]{6}$")]
    public string? Color { get; init; }

    /// <summary>
    /// New role; the built-in admin profile always stays an admin.
    /// </summary>
    public RoleDto? Role { get; init; }
}
