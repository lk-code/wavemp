namespace WaveMP.Service.Dtos;

/// <summary>
/// An album of the library.
/// </summary>
public sealed class AlbumDto
{
    public required Guid Id { get; init; }

    public required string Title { get; init; }

    /// <summary>
    /// Album artist; null for compilations and albums without a known artist.
    /// </summary>
    public ArtistReferenceDto? Artist { get; init; }

    /// <summary>
    /// Tracks of several artists without a common album artist. Clients show their own text for it.
    /// </summary>
    public required bool IsCompilation { get; init; }

    public int? Year { get; init; }

    /// <summary>
    /// The first genre of its tracks in the order of disc and track number.
    /// </summary>
    public string? Genre { get; init; }

    public required int TrackCount { get; init; }

    /// <summary>
    /// Sum of the durations of the tracks in milliseconds.
    /// </summary>
    public required long DurationMilliseconds { get; init; }

    /// <summary>
    /// Whether the album or one of its tracks has a cover (GetAlbumCover).
    /// </summary>
    public required bool HasCover { get; init; }

    /// <summary>
    /// When the album was found for the first time.
    /// </summary>
    public required DateTimeOffset FirstSeenAt { get; init; }
}
