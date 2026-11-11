using System.Diagnostics.CodeAnalysis;
using WaveMP.Data.Entities;

namespace WaveMP.Core.Users.Users;

/// <summary>
/// Result of creating or changing a user: the saved user or the reason why nothing was saved.
/// </summary>
public sealed record UserResult(User? User, UserError? Error)
{
    [MemberNotNullWhen(true, nameof(User))]
    public bool IsSuccess => User is not null;

    public static UserResult Success(User user)
    {
        return new UserResult(user, null);
    }

    public static UserResult Failure(UserError error)
    {
        return new UserResult(null, error);
    }
}
