namespace WaveMP.Data.Projections;

/// <summary>
/// A track with its artist and album.
/// </summary>
/// <param name="Id">Id of the track.</param>
/// <param name="Title">Title of the track.</param>
/// <param name="ArtistId">Id of the artist of the track.</param>
/// <param name="ArtistName">Name of the artist of the track.</param>
/// <param name="AlbumId">Id of the album.</param>
/// <param name="AlbumTitle">Title of the album.</param>
/// <param name="AlbumArtistId">Id of the album artist.</param>
/// <param name="AlbumArtistName">Name of the album artist.</param>
/// <param name="AlbumIsCompilation">Whether the album is a compilation.</param>
/// <param name="TrackNumber">Number of the track on its disc.</param>
/// <param name="DiscNumber">Number of the disc.</param>
/// <param name="Year">Year of the track.</param>
/// <param name="Genre">Genre of the track.</param>
/// <param name="Duration">Duration of the primary audio file, otherwise of the primary video file.</param>
/// <param name="HasAudio">Whether an audio file exists.</param>
/// <param name="HasVideo">Whether a video file exists.</param>
/// <param name="HasCover">Whether a cover exists: the track's own, its album's or one of another track of the album.
/// </param>
/// <param name="FirstSeenAt">When the track was found for the first time.</param>
/// <param name="IsFavorite">Whether the user of the query marked the track as favorite.</param>
/// <param name="Rating">Rating of the user of the query, 1 to 5 stars; 0 if the user did not rate the track.</param>
public sealed record TrackRow(
    Guid Id,
    string Title,
    Guid? ArtistId,
    string? ArtistName,
    Guid? AlbumId,
    string? AlbumTitle,
    Guid? AlbumArtistId,
    string? AlbumArtistName,
    bool AlbumIsCompilation,
    int? TrackNumber,
    int? DiscNumber,
    int? Year,
    string? Genre,
    TimeSpan? Duration,
    bool HasAudio,
    bool HasVideo,
    bool HasCover,
    DateTimeOffset FirstSeenAt,
    bool IsFavorite = false,
    int Rating = 0);
