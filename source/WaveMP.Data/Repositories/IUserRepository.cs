using WaveMP.Data.Entities;
using WaveMP.Data.Queries;

namespace WaveMP.Data.Repositories;

/// <summary>
/// Access to the users and their settings.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Lists the users sorted and filtered as requested; ties are sorted by id.
    /// </summary>
    Task<Page<User>> ListAsync(UserListQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the profiles for the profile selection: in the order they were created, the built-in admin last.
    /// </summary>
    Task<Page<User>> ListProfilesAsync(int offset, int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a user with its settings. The user is tracked, so changes can be saved with <see cref="UpdateAsync"/>.
    /// </summary>
    Task<User?> FindAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a user other than <paramref name="exceptId"/> has the normalized name.
    /// </summary>
    Task<bool> NameExistsAsync(
        string normalizedName,
        Guid? exceptId = null,
        CancellationToken cancellationToken = default);

    Task<bool> BuiltInAdminExistsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Colors of all users, one entry per user.
    /// </summary>
    Task<IReadOnlyList<string>> ListColorsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a user with its settings.
    /// </summary>
    /// <exception cref="UserNameConflictException">Another user has the same name.</exception>
    Task AddAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the changes of a user loaded with <see cref="FindAsync"/>.
    /// </summary>
    /// <exception cref="UserNameConflictException">Another user has the same name.</exception>
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a user loaded with <see cref="FindAsync"/>, together with its settings and sessions.
    /// </summary>
    Task DeleteAsync(User user, CancellationToken cancellationToken = default);
}
