namespace WaveMP.Core.Users.Users;

/// <summary>
/// Reasons why a change of a user was rejected.
/// </summary>
public enum UserError
{
    /// <summary>
    /// The user does not exist.
    /// </summary>
    NotFound,

    /// <summary>
    /// Another user has the same name (without regard to case).
    /// </summary>
    NameTaken,

    /// <summary>
    /// The built-in admin profile can neither be deleted nor lose the admin role.
    /// </summary>
    BuiltInAdmin,

    InvalidName,

    InvalidInitials,

    InvalidColor,
}
