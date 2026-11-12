using WaveMP.Core.Collection.Favorites;
using WaveMP.Core.Library.Albums;
using WaveMP.Core.Playlists.Histories;
using WaveMP.Core.Playlists.Playlists;
using WaveMP.Data.Projections;
using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Collection.HomePages;

internal sealed class HomePageService(
    IPlaylistService playlists,
    IRecentlyPlayedService recentlyPlayed,
    IAlbumService albums,
    IFavoriteService favorites,
    ITrackRepository tracks) : IHomePageService
{
    /// <summary>
    /// Lowest rating of the row "top rated": every rated track.
    /// </summary>
    private const int TopRatedMinRating = 1;

    public async Task<HomePage> GetAsync(Guid userId, int limit, CancellationToken cancellationToken = default)
    {
        // One after the other: the services share the database context of the request.
        Page<PlaylistItem> ownPlaylists = await playlists.ListAsync(
            new PlaylistListQuery(userId, 0, limit, PlaylistSortField.UpdatedAt, SortDirection.Desc),
            cancellationToken);
        Page<RecentlyPlayedItem> played = await recentlyPlayed.ListAsync(userId, 0, limit, cancellationToken);
        Page<AlbumRow> added = await albums.ListAsync(
            new AlbumListRequest(0, limit, AlbumSortField.FirstSeenAt, SortDirection.Desc, UserId: userId),
            cancellationToken);
        Page<FavoriteItem> favorite = await favorites.ListAsync(
            new FavoriteListRequest(userId, 0, limit),
            cancellationToken);
        Page<TrackRow> topRated = await tracks.ListAsync(
            new TrackListQuery(
                0,
                limit,
                TrackSortField.Rating,
                SortDirection.Desc,
                UserId: userId,
                MinRating: TopRatedMinRating),
            cancellationToken);

        return new HomePage(ownPlaylists, played, added, favorite, topRated);
    }
}
