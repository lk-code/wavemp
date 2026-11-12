namespace WaveMP.Data.Projections;

/// <summary>
/// An artist with the number of its albums and tracks.
/// </summary>
/// <param name="Id">Id of the artist.</param>
/// <param name="Name">Name as first found in the media files.</param>
/// <param name="AlbumCount">Albums of the artist: albums with the artist as album artist or with tracks of the artist.
/// </param>
/// <param name="TrackCount">Tracks of the artist: tracks of the artist or on albums of the artist.</param>
/// <param name="FirstSeenAt">When the artist was found for the first time.</param>
/// <param name="IsFavorite">Whether the user of the query marked the artist as favorite.</param>
public sealed record ArtistRow(
    Guid Id,
    string Name,
    int AlbumCount,
    int TrackCount,
    DateTimeOffset FirstSeenAt,
    bool IsFavorite = false);
