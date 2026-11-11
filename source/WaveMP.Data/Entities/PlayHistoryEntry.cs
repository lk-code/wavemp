namespace WaveMP.Data.Entities;

/// <summary>
/// One play of a track by a user: the client reports it once the track has played long enough. The entry disappears
/// with its user and with its track (for example when a rescan no longer finds the track). Source of "recently played"
/// and, later, of scrobbling.
/// </summary>
public class PlayHistoryEntry
{
    public Guid Id { get; set; }

    public required Guid UserId { get; set; }

    public User? User { get; set; }

    public required Guid TrackId { get; set; }

    public Track? Track { get; set; }

    /// <summary>
    /// When the playback of the track started.
    /// </summary>
    public required DateTimeOffset PlayedAt { get; set; }
}
