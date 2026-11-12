namespace WaveMP.Service.Dtos;

/// <summary>
/// A favorite of the current profile: a track, an album or an artist. Exactly the member of its type is set.
/// </summary>
public sealed class FavoriteDto
{
    public required FavoriteTypeDto Type { get; init; }

    /// <summary>
    /// Id of the track, album or artist.
    /// </summary>
    public required Guid ItemId { get; init; }

    /// <summary>
    /// When the profile marked it.
    /// </summary>
    public required DateTimeOffset AddedAt { get; init; }

    /// <summary>
    /// The track of a track favorite; null otherwise.
    /// </summary>
    public TrackDto? Track { get; init; }

    /// <summary>
    /// The album of an album favorite; null otherwise.
    /// </summary>
    public AlbumDto? Album { get; init; }

    /// <summary>
    /// The artist of an artist favorite; null otherwise.
    /// </summary>
    public ArtistDto? Artist { get; init; }
}
