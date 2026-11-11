using WaveMP.Data.Queries;

namespace WaveMP.Core.Playlists.Histories;

/// <summary>
/// The automatic list of the tracks a user played recently, made from the play history.
/// </summary>
public interface IRecentlyPlayedService
{
    /// <summary>
    /// Lists the played tracks, each once, the most recently played first; the list holds at most the configured
    /// number of tracks (100 by default).
    /// </summary>
    Task<Page<RecentlyPlayedItem>> ListAsync(
        Guid userId,
        int offset,
        int limit,
        CancellationToken cancellationToken = default);
}
