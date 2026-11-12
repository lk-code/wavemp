namespace WaveMP.Core.Collection.Ratings;

/// <summary>
/// The ratings of tracks by the profiles.
/// </summary>
public interface IRatingService
{
    /// <summary>
    /// Sets the rating of the profile for a track: 1 to 5 stars, 0 removes the rating.
    /// </summary>
    Task<RatingStatus> SetAsync(Guid userId, Guid trackId, int rating, CancellationToken cancellationToken = default);
}
