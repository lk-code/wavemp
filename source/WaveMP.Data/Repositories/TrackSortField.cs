namespace WaveMP.Data.Repositories;

/// <summary>
/// Sort key of the track list. Keys that group tracks by album keep the tracks of an album together, in the order of
/// disc and track number.
/// </summary>
public enum TrackSortField
{
    /// <summary>
    /// Album artist (for tracks without album: their artist), then album, disc and track number.
    /// </summary>
    AlbumArtist,

    /// <summary>
    /// Album, then disc and track number.
    /// </summary>
    Album,

    Title,

    /// <summary>
    /// Artist of the track, then album, disc and track number.
    /// </summary>
    Artist,

    Duration,

    /// <summary>
    /// Year, then album, disc and track number.
    /// </summary>
    Year,

    /// <summary>
    /// Genre, then album, disc and track number.
    /// </summary>
    Genre,

    /// <summary>
    /// When the track was found for the first time, then album, disc and track number.
    /// </summary>
    FirstSeenAt,

    /// <summary>
    /// How well title, artist and album match the search text; only with a search text.
    /// </summary>
    Relevance,
}
