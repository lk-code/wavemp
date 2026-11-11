namespace WaveMP.Data.Entities;

/// <summary>
/// A track in a playlist. The entry disappears with its playlist and with its track (for example when a rescan no
/// longer finds the track).
/// </summary>
public class PlaylistEntry
{
    public Guid Id { get; set; }

    public required Guid PlaylistId { get; set; }

    public Playlist? Playlist { get; set; }

    public required Guid TrackId { get; set; }

    public Track? Track { get; set; }

    /// <summary>
    /// Place of the entry in the playlist, starting at 0. Entries removed by the database (with their track) leave
    /// gaps until the next change of the playlist closes them; the order is position, then id.
    /// </summary>
    public required int Position { get; set; }

    public required DateTimeOffset AddedAt { get; set; }
}
