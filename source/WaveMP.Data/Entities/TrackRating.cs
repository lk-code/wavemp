namespace WaveMP.Data.Entities;

/// <summary>
/// The rating of a track by a user, 1 to 5 stars; a track without rating has no row. The rating disappears with its
/// user and with its track.
/// </summary>
public class TrackRating
{
    public required Guid UserId { get; set; }

    public User? User { get; set; }

    public required Guid TrackId { get; set; }

    public Track? Track { get; set; }

    /// <summary>
    /// Number of stars, 1 to 5.
    /// </summary>
    public required int Rating { get; set; }

    /// <summary>
    /// When the rating was set or changed for the last time.
    /// </summary>
    public required DateTimeOffset UpdatedAt { get; set; }
}
