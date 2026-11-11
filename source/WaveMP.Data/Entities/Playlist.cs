namespace WaveMP.Data.Entities;

/// <summary>
/// A playlist of a user: tracks in an order of the user's choice, a track may appear more than once. The playlist
/// disappears with its user.
/// </summary>
public class Playlist
{
    public Guid Id { get; set; }

    public required Guid UserId { get; set; }

    public User? User { get; set; }

    /// <summary>
    /// Name of the playlist; names do not have to be unique.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Upper-case form of <see cref="Name"/> for sorting without regard to case.
    /// </summary>
    public required string NormalizedName { get; set; }

    /// <summary>
    /// When the own cover was uploaded; <c>null</c> without an own cover. Also the version of the cover.
    /// </summary>
    public DateTimeOffset? CoverUpdatedAt { get; set; }

    public required DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// When the name, the cover or the entries changed for the last time.
    /// </summary>
    public required DateTimeOffset UpdatedAt { get; set; }

    public ICollection<PlaylistEntry> Entries { get; set; } = [];
}
