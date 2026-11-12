namespace WaveMP.Service.Dtos;

/// <summary>
/// The home page of the current profile: every row is the first page of its list, total tells how many items the
/// whole list has.
/// </summary>
public sealed class HomeDto
{
    /// <summary>
    /// The own playlists, the last changed first (ListMyPlaylists).
    /// </summary>
    public required PageDto<PlaylistDto> Playlists { get; init; }

    /// <summary>
    /// The tracks played last (ListMyRecentlyPlayedTracks).
    /// </summary>
    public required PageDto<RecentlyPlayedTrackDto> RecentlyPlayed { get; init; }

    /// <summary>
    /// The albums found last (ListAlbums with sort FirstSeenAt).
    /// </summary>
    public required PageDto<AlbumDto> RecentlyAdded { get; init; }

    /// <summary>
    /// The favorite tracks, albums and artists, the newest first (ListMyFavorites).
    /// </summary>
    public required PageDto<FavoriteDto> Favorites { get; init; }

    /// <summary>
    /// Only the rated tracks, the most stars first, then the latest rating (like ListTracks with sort Rating).
    /// </summary>
    public required PageDto<TrackDto> TopRated { get; init; }
}
