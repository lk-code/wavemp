using AwesomeAssertions;
using NSubstitute;
using WaveMP.Core.Collection.Favorites;
using WaveMP.Data.Entities;
using WaveMP.Data.Projections;
using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Collection.Tests.Favorites;

[TestFixture]
public class FavoriteServiceTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 12, 10, 30, 0, TimeSpan.Zero);

    private readonly Guid _userId = Guid.NewGuid();
    private IFavoriteRepository _favorites = null!;
    private ITrackRepository _tracks = null!;
    private IAlbumRepository _albums = null!;
    private IArtistRepository _artists = null!;
    private FavoriteService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _favorites = Substitute.For<IFavoriteRepository>();
        _tracks = Substitute.For<ITrackRepository>();
        _albums = Substitute.For<IAlbumRepository>();
        _artists = Substitute.For<IArtistRepository>();
        _service = new FavoriteService(_favorites, _tracks, _albums, _artists, Clock());
    }

    [TestCase(FavoriteType.Track)]
    [TestCase(FavoriteType.Album)]
    [TestCase(FavoriteType.Artist)]
    public async Task AddAsync_Target_AddsAFavoriteWithOnlyTheIdOfItsType(FavoriteType type)
    {
        var itemId = Guid.NewGuid();
        _favorites.AddAsync(Arg.Any<Favorite>(), Arg.Any<CancellationToken>()).Returns(FavoriteChangeStatus.Added);

        FavoriteStatus status = await _service.AddAsync(_userId, type, itemId);

        status.Should().Be(FavoriteStatus.Done);
        await _favorites.Received(1).AddAsync(
            Arg.Is<Favorite>(favorite => favorite.UserId == _userId
                && favorite.Type == type
                && favorite.CreatedAt == _now
                && favorite.TrackId == (type == FavoriteType.Track ? itemId : null)
                && favorite.AlbumId == (type == FavoriteType.Album ? itemId : null)
                && favorite.ArtistId == (type == FavoriteType.Artist ? itemId : null)),
            Arg.Any<CancellationToken>());
    }

    [TestCase(FavoriteChangeStatus.Added, FavoriteStatus.Done)]
    [TestCase(FavoriteChangeStatus.Existing, FavoriteStatus.Done)]
    [TestCase(FavoriteChangeStatus.TargetNotFound, FavoriteStatus.NotFound)]
    public async Task AddAsync_Always_MapsTheOutcome(FavoriteChangeStatus change, FavoriteStatus expected)
    {
        _favorites.AddAsync(Arg.Any<Favorite>(), Arg.Any<CancellationToken>()).Returns(change);

        FavoriteStatus status = await _service.AddAsync(_userId, FavoriteType.Track, Guid.NewGuid());

        status.Should().Be(expected);
    }

    [Test]
    public async Task RemoveAsync_Always_RemovesTheFavoriteOfTheUser()
    {
        var itemId = Guid.NewGuid();

        await _service.RemoveAsync(_userId, FavoriteType.Album, itemId);

        await _favorites.Received(1).RemoveAsync(_userId, FavoriteType.Album, itemId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ListAsync_Always_LoadsTheTargetsInTheOrderOfTheFavoritesAndLeavesOutRemovedOnes()
    {
        TrackRow track = Track();
        AlbumRow album = Album();
        ArtistRow artist = new(Guid.NewGuid(), "Alpha Band", 1, 2, _now, true);
        FavoriteRow trackFavorite = new(Guid.NewGuid(), FavoriteType.Track, track.Id, _now);
        FavoriteRow goneFavorite = new(Guid.NewGuid(), FavoriteType.Track, Guid.NewGuid(), _now.AddMinutes(-1));
        FavoriteRow artistFavorite = new(Guid.NewGuid(), FavoriteType.Artist, artist.Id, _now.AddMinutes(-2));
        FavoriteRow albumFavorite = new(Guid.NewGuid(), FavoriteType.Album, album.Id, _now.AddMinutes(-3));
        _favorites.ListAsync(new FavoriteListQuery(_userId, 5, 10, null), Arg.Any<CancellationToken>())
            .Returns(new Page<FavoriteRow>([trackFavorite, goneFavorite, artistFavorite, albumFavorite], 5, 10, 24));
        _tracks.ListByIdsAsync(
                Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 2),
                _userId,
                Arg.Any<CancellationToken>())
            .Returns([track]);
        _albums.ListByIdsAsync(
                Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Single() == album.Id),
                _userId,
                Arg.Any<CancellationToken>())
            .Returns([album]);
        _artists.ListByIdsAsync(
                Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Single() == artist.Id),
                _userId,
                Arg.Any<CancellationToken>())
            .Returns([artist]);

        Page<FavoriteItem> page = await _service.ListAsync(new FavoriteListRequest(_userId, 5, 10));

        page.Items.Should().Equal(
            new FavoriteItem(trackFavorite, track, null, null),
            new FavoriteItem(artistFavorite, null, null, artist),
            new FavoriteItem(albumFavorite, null, album, null));
        page.Offset.Should().Be(5);
        page.Limit.Should().Be(10);
        page.Total.Should().Be(24);
    }

    [Test]
    public async Task ListAsync_WithType_PassesTheType()
    {
        _favorites.ListAsync(Arg.Any<FavoriteListQuery>(), Arg.Any<CancellationToken>())
            .Returns(new Page<FavoriteRow>([], 0, 20, 0));

        await _service.ListAsync(new FavoriteListRequest(_userId, 0, 20, FavoriteType.Artist));

        await _favorites.Received(1).ListAsync(
            new FavoriteListQuery(_userId, 0, 20, FavoriteType.Artist),
            Arg.Any<CancellationToken>());
    }

    private static TimeProvider Clock()
    {
        TimeProvider clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(_now);
        return clock;
    }

    private static TrackRow Track()
    {
        return new TrackRow(
            Guid.NewGuid(),
            "Opening",
            null,
            null,
            null,
            null,
            null,
            null,
            false,
            null,
            null,
            null,
            null,
            null,
            true,
            false,
            false,
            _now,
            true,
            4);
    }

    private static AlbumRow Album()
    {
        return new AlbumRow(Guid.NewGuid(), "First Album", null, null, false, 2001, null, 3, 0, false, _now, true);
    }
}
