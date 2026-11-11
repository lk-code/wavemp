using WaveMP.Data.Entities;
using WaveMP.Data.Queries;

namespace WaveMP.Data.Repositories;

/// <summary>
/// Access to the play history of the users. Nothing is tracked.
/// </summary>
public interface IPlayHistoryRepository
{
    Task AddAsync(PlayHistoryEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the entries of a user, the newest play first.
    /// </summary>
    Task<Page<PlayHistoryEntry>> ListAsync(PlayHistoryListQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds an entry of the user; entries of other users are not found.
    /// </summary>
    Task<PlayHistoryEntry?> FindAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);
}
