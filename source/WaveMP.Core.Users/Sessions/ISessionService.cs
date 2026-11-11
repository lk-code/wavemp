namespace WaveMP.Core.Users.Sessions;

/// <summary>
/// Creates, finds and ends the sessions of the profiles.
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// Selects a profile: creates a session for it. Returns <c>null</c> if the profile does not exist.
    /// Expired sessions of all profiles are removed on the way.
    /// </summary>
    Task<ActiveSession?> CreateAsync(Guid profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a session that has not expired, or <c>null</c>.
    /// </summary>
    Task<ActiveSession?> GetActiveAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ends a session; its token is no longer accepted.
    /// </summary>
    Task DeleteAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
