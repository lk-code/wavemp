using WaveMP.Data.Entities;
using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Users.Users;

/// <summary>
/// Lists, creates, changes and deletes users (profiles).
/// </summary>
public interface IUserService
{
    Task<Page<User>> ListAsync(UserListQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Profiles for the profile selection, in the order they were created; the built-in admin last.
    /// </summary>
    Task<Page<User>> ListProfilesAsync(int offset, int limit, CancellationToken cancellationToken = default);

    Task<User?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<UserResult> CreateAsync(CreateUserCommand command, CancellationToken cancellationToken = default);

    Task<UserResult> UpdateAsync(Guid id, UpdateUserCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a user with its settings and sessions. Returns <c>null</c> on success, otherwise the reason.
    /// </summary>
    Task<UserError?> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
