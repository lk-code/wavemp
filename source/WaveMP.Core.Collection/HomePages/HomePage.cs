using WaveMP.Core.Collection.Favorites;
using WaveMP.Core.Playlists.Histories;
using WaveMP.Core.Playlists.Playlists;
using WaveMP.Data.Projections;
using WaveMP.Data.Queries;

namespace WaveMP.Core.Collection.HomePages;

/// <summary>
/// The rows of the home page of a profile; each row is the first page of its list.
/// </summary>
/// <param name="Playlists">The own playlists, the last changed first.</param>
/// <param name="RecentlyPlayed">The tracks played last.</param>
/// <param name="RecentlyAdded">The albums found last.</param>
/// <param name="Favorites">The favorite tracks, albums and artists, the newest first.</param>
/// <param name="TopRated">The rated tracks, the most stars first, then the latest rating.</param>
public sealed record HomePage(
    Page<PlaylistItem> Playlists,
    Page<RecentlyPlayedItem> RecentlyPlayed,
    Page<AlbumRow> RecentlyAdded,
    Page<FavoriteItem> Favorites,
    Page<TrackRow> TopRated);
