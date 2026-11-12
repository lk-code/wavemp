using AwesomeAssertions;
using NSubstitute;
using WaveMP.Core.Collection.Favorites;
using WaveMP.Core.Collection.HomePages;
using WaveMP.Core.Library.Albums;
using WaveMP.Core.Playlists.Histories;
using WaveMP.Core.Playlists.Playlists;
using WaveMP.Data.Projections;
using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Collection.Tests.HomePages;

[TestFixture]
public class HomePageServiceTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private IPlaylistService _playlists = null!;
    private IRecentlyPlayedService _recentlyPlayed = null!;
    private IAlbumService _albums = null!;
    private IFavoriteService _favorites = null!;
    private ITrackRepository _tracks = null!;
    private HomePageService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _playlists = Substitute.For<IPlaylistService>();
        _recentlyPlayed = Substitute.For<IRecentlyPlayedService>();
        _albums = Substitute.For<IAlbumService>();
        _favorites = Substitute.For<IFavoriteService>();
        _tracks = Substitute.For<ITrackRepository>();
        _service = new HomePageService(_playlists, _recentlyPlayed, _albums, _favorites, _tracks);
    }

    [Test]
    public async Task GetAsync_Always_LoadsEveryRowOfTheUserWithTheLimit()
    {
        Page<PlaylistItem> playlists = new([], 0, 7, 1);
        Page<RecentlyPlayedItem> played = new([], 0, 7, 2);
        Page<AlbumRow> added = new([], 0, 7, 3);
        Page<FavoriteItem> favorites = new([], 0, 7, 4);
        Page<TrackRow> topRated = new([], 0, 7, 5);
        _playlists.ListAsync(
                new PlaylistListQuery(_userId, 0, 7, PlaylistSortField.UpdatedAt, SortDirection.Desc),
                Arg.Any<CancellationToken>())
            .Returns(playlists);
        _recentlyPlayed.ListAsync(_userId, 0, 7, Arg.Any<CancellationToken>()).Returns(played);
        _albums.ListAsync(
                new AlbumListRequest(0, 7, AlbumSortField.FirstSeenAt, SortDirection.Desc, UserId: _userId),
                Arg.Any<CancellationToken>())
            .Returns(added);
        _favorites.ListAsync(new FavoriteListRequest(_userId, 0, 7), Arg.Any<CancellationToken>())
            .Returns(favorites);
        _tracks.ListAsync(
                new TrackListQuery(
                    0,
                    7,
                    TrackSortField.Rating,
                    SortDirection.Desc,
                    UserId: _userId,
                    MinRating: 1),
                Arg.Any<CancellationToken>())
            .Returns(topRated);

        HomePage page = await _service.GetAsync(_userId, 7);

        page.Should().Be(new HomePage(playlists, played, added, favorites, topRated));
    }
}
