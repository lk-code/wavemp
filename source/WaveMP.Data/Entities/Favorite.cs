namespace WaveMP.Data.Entities;

/// <summary>
/// A track, album or artist a user marked as favorite. Exactly the id of the <see cref="Type"/> is set. The favorite
/// disappears with its user and with its target (for example when a rescan no longer finds the track, album or
/// artist).
/// </summary>
public class Favorite
{
    public Guid Id { get; set; }

    public required Guid UserId { get; set; }

    public User? User { get; set; }

    public required FavoriteType Type { get; set; }

    public Guid? TrackId { get; set; }

    public Track? Track { get; set; }

    public Guid? AlbumId { get; set; }

    public Album? Album { get; set; }

    public Guid? ArtistId { get; set; }

    public Artist? Artist { get; set; }

    /// <summary>
    /// When the user marked the target; marking it again keeps the time.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; set; }
}
