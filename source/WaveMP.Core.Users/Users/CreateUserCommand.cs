using WaveMP.Data.Entities;

namespace WaveMP.Core.Users.Users;

/// <summary>
/// Creates a user.
/// </summary>
/// <param name="Name">Name of the profile.</param>
/// <param name="Initials">Own initials; <c>null</c> or empty to derive them from the name.</param>
/// <param name="Color">Color as <c>#rrggbb</c>; <c>null</c> for the least used color of the palette.</param>
/// <param name="Role">Role; <c>null</c> for <see cref="UserRole.User"/>.</param>
public sealed record CreateUserCommand(
    string Name,
    string? Initials = null,
    string? Color = null,
    UserRole? Role = null);
