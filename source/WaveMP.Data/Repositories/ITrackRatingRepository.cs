namespace WaveMP.Data.Repositories;

/// <summary>
/// The ratings of tracks by the users. Nothing is tracked.
/// </summary>
public interface ITrackRatingRepository
{
    /// <summary>
    /// Sets the rating (1 to 5 stars) of the user for the track; an unchanged rating keeps its time. Returns
    /// <c>false</c> if the track does not exist.
    /// </summary>
    Task<bool> SetAsync(
        Guid userId,
        Guid trackId,
        int rating,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the rating of the user for the track, also if there is none. Returns <c>false</c> if the track does
    /// not exist.
    /// </summary>
    Task<bool> RemoveAsync(Guid userId, Guid trackId, CancellationToken cancellationToken = default);
}
