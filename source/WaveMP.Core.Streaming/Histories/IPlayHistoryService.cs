using WaveMP.Data.Queries;

namespace WaveMP.Core.Streaming.Histories;

/// <summary>
/// The play history of the users: clients report every play once it has played long enough.
/// </summary>
public interface IPlayHistoryService
{
    /// <summary>
    /// Records a play of a track. <paramref name="playedAt"/> is the start of the play, by default now; it must not
    /// lie in the future (beyond the allowed clock skew) or be older than the configured maximum age.
    /// </summary>
    Task<PlayHistoryResult> RecordAsync(
        Guid userId,
        Guid trackId,
        DateTimeOffset? playedAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the plays of the user, the newest first.
    /// </summary>
    Task<Page<PlayHistoryItem>> ListAsync(
        Guid userId,
        int offset,
        int limit,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// An entry of the user, or <c>null</c>; entries of other users are not found.
    /// </summary>
    Task<PlayHistoryItem?> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken = default);
}
