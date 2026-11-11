using WaveMP.Data.Entities;

namespace WaveMP.Core.Users.Users;

/// <summary>
/// Changes a user. Every <c>null</c> value keeps the current value.
/// </summary>
/// <param name="Name">New name. Initials that were derived from the old name follow the new name.</param>
/// <param name="Initials">Own initials; an empty string derives them from the name again.</param>
/// <param name="Color">Color as <c>#rrggbb</c>.</param>
/// <param name="Role">New role; the built-in admin profile always stays an admin.</param>
public sealed record UpdateUserCommand(
    string? Name = null,
    string? Initials = null,
    string? Color = null,
    UserRole? Role = null);
