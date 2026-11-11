namespace WaveMP.Service.Dtos;

/// <summary>
/// An artist of the library.
/// </summary>
public sealed class ArtistDto
{
    public required Guid Id { get; init; }

    /// <summary>
    /// Name as first found in the media files.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Albums with the artist as album artist or with tracks of the artist.
    /// </summary>
    public required int AlbumCount { get; init; }

    /// <summary>
    /// Tracks of the artist or on albums of the artist.
    /// </summary>
    public required int TrackCount { get; init; }

    /// <summary>
    /// When the artist was found for the first time.
    /// </summary>
    public required DateTimeOffset FirstSeenAt { get; init; }
}
