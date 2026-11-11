namespace WaveMP.Data.Projections;

/// <summary>
/// An album with its album artist and the summary of its tracks.
/// </summary>
/// <param name="Id">Id of the album.</param>
/// <param name="Title">Title of the album.</param>
/// <param name="ArtistId">Id of the album artist; <c>null</c> for compilations and unknown artists.</param>
/// <param name="ArtistName">Name of the album artist.</param>
/// <param name="IsCompilation">Tracks of several artists without a common album artist.</param>
/// <param name="Year">Year of the album.</param>
/// <param name="Genre">The first genre of its tracks in the order of disc and track number.</param>
/// <param name="TrackCount">Number of tracks.</param>
/// <param name="DurationMilliseconds">Sum of the durations of the tracks.</param>
/// <param name="HasCover">Whether the album or one of its tracks has a cover.</param>
/// <param name="FirstSeenAt">When the album was found for the first time.</param>
public sealed record AlbumRow(
    Guid Id,
    string Title,
    Guid? ArtistId,
    string? ArtistName,
    bool IsCompilation,
    int? Year,
    string? Genre,
    int TrackCount,
    double DurationMilliseconds,
    bool HasCover,
    DateTimeOffset FirstSeenAt);
