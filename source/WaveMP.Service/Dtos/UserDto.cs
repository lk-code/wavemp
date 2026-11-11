namespace WaveMP.Service.Dtos;

/// <summary>
/// A user (profile) as managed by admins.
/// </summary>
public sealed class UserDto
{
    public required Guid Id { get; init; }

    /// <summary>
    /// Name of the profile.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Initials shown on the profile tile.
    /// </summary>
    public required string Initials { get; init; }

    /// <summary>
    /// Color of the profile as #rrggbb.
    /// </summary>
    public required string Color { get; init; }

    public required RoleDto Role { get; init; }

    /// <summary>
    /// True for the built-in admin profile, which cannot be deleted and always stays an admin.
    /// </summary>
    public required bool IsBuiltIn { get; init; }

    /// <summary>
    /// When the profile was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }
}
