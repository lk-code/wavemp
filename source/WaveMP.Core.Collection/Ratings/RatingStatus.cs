namespace WaveMP.Core.Collection.Ratings;

/// <summary>
/// Outcome of rating a track.
/// </summary>
public enum RatingStatus
{
    Done,

    /// <summary>
    /// The track does not exist.
    /// </summary>
    NotFound,

    /// <summary>
    /// The rating lies outside <see cref="TrackRatings.Min"/> and <see cref="TrackRatings.Max"/>.
    /// </summary>
    Invalid,
}
