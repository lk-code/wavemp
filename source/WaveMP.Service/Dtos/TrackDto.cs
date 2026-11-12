namespace WaveMP.Service.Dtos;

/// <summary>
/// A track of the library: all media files with the same base name in one folder.
/// </summary>
public sealed class TrackDto
{
    public required Guid Id { get; init; }

    public required string Title { get; init; }

    /// <summary>
    /// Artist of the track; null if unknown.
    /// </summary>
    public ArtistReferenceDto? Artist { get; init; }

    /// <summary>
    /// Album of the track; null for tracks without album.
    /// </summary>
    public AlbumReferenceDto? Album { get; init; }

    /// <summary>
    /// Number of the track on its disc.
    /// </summary>
    public int? TrackNumber { get; init; }

    public int? DiscNumber { get; init; }

    public int? Year { get; init; }

    public string? Genre { get; init; }

    /// <summary>
    /// Duration in milliseconds: of the audio file, otherwise of the video file.
    /// </summary>
    public long? DurationMilliseconds { get; init; }

    /// <summary>
    /// Whether the track can be played as audio.
    /// </summary>
    public required bool HasAudio { get; init; }

    /// <summary>
    /// Whether the track has a music video.
    /// </summary>
    public required bool HasVideo { get; init; }

    /// <summary>
    /// Whether a cover exists (GetTrackCover): of the track, of its album or of another track of its album.
    /// </summary>
    public required bool HasCover { get; init; }

    /// <summary>
    /// When the track was found for the first time ("recently added").
    /// </summary>
    public required DateTimeOffset FirstSeenAt { get; init; }

    /// <summary>
    /// Whether the current profile marked the track as favorite.
    /// </summary>
    public required bool IsFavorite { get; init; }

    /// <summary>
    /// Rating of the current profile: 1 to 5 stars, 0 if not rated.
    /// </summary>
    public required int Rating { get; init; }
}
