using AwesomeAssertions;
using NSubstitute;
using WaveMP.Core.Library.Albums;
using WaveMP.Core.Library.Tracks;
using WaveMP.Data.Projections;
using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Library.Tests.Albums;

[TestFixture]
public class AlbumServiceTests
{
    private IAlbumRepository _albums = null!;
    private ITrackRepository _tracks = null!;
    private AlbumService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _albums = Substitute.For<IAlbumRepository>();
        _tracks = Substitute.For<ITrackRepository>();
        _service = new AlbumService(_albums, _tracks);
    }

    [Test]
    public async Task ListAsync_WithoutSort_SortsByArtistAndPassesTheFilters()
    {
        var artistId = Guid.NewGuid();

        await _service.ListAsync(new AlbumListRequest(0, 50, ArtistId: artistId, Genre: "Pop", Year: 2010));

        await _albums.Received(1).ListAsync(
            new AlbumListQuery(0, 50, AlbumSortField.Artist, SortDirection.Asc, null, artistId, "Pop", 2010),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ListAsync_WithUserAndMyMusic_PassesBoth()
    {
        var userId = Guid.NewGuid();

        await _service.ListAsync(new AlbumListRequest(0, 50, UserId: userId, MyMusic: true));

        await _albums.Received(1).ListAsync(
            new AlbumListQuery(0, 50, AlbumSortField.Artist, SortDirection.Asc, UserId: userId, MyMusic: true),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetAsync_WithUser_FindsTheAlbumOfTheUser()
    {
        var userId = Guid.NewGuid();
        var albumId = Guid.NewGuid();

        await _service.GetAsync(albumId, userId);

        await _albums.Received(1).FindAsync(albumId, userId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ListTracksAsync_UnknownAlbum_ReturnsNull()
    {
        Page<TrackRow>? page = await _service.ListTracksAsync(Guid.NewGuid(), new TrackListRequest(0, 50));

        page.Should().BeNull();
        await _tracks.DidNotReceiveWithAnyArgs().ListAsync(default!);
    }

    [Test]
    public async Task ListTracksAsync_KnownAlbum_ListsItsTracksByDiscAndNumber()
    {
        var albumId = Guid.NewGuid();
        _albums.ExistsAsync(albumId, Arg.Any<CancellationToken>()).Returns(true);

        await _service.ListTracksAsync(albumId, new TrackListRequest(0, 100, AlbumId: Guid.NewGuid()));

        await _tracks.Received(1).ListAsync(
            new TrackListQuery(0, 100, TrackSortField.Album, SortDirection.Asc, AlbumId: albumId),
            Arg.Any<CancellationToken>());
    }
}
