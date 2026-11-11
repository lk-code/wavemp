using WaveMP.Data.Entities;

namespace WaveMP.Data.Repositories;

/// <summary>
/// Access to the sessions of the profiles.
/// </summary>
public interface ISessionRepository
{
    Task AddAsync(Session session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a session that has not expired at <paramref name="now"/>, with its user. Nothing is tracked.
    /// </summary>
    Task<Session?> FindActiveAsync(Guid sessionId, DateTimeOffset now, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all sessions that have expired at <paramref name="now"/> and returns their number.
    /// </summary>
    Task<int> DeleteExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken = default);
}
