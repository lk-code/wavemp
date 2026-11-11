using AwesomeAssertions;
using NSubstitute;
using WaveMP.Core.Library.Albums;
using WaveMP.Core.Library.Artists;
using WaveMP.Data.Projections;
using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Library.Tests.Artists;

[TestFixture]
public class ArtistServiceTests
{
    private IArtistRepository _artists = null!;
    private IAlbumRepository _albums = null!;
    private ArtistService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _artists = Substitute.For<IArtistRepository>();
        _albums = Substitute.For<IAlbumRepository>();
        _service = new ArtistService(_artists, _albums);
    }

    [Test]
    public async Task ListAsync_WithSearchAndName_KeepsTheRequestedKey()
    {
        await _service.ListAsync(new ArtistListRequest(0, 20, ArtistSortField.Name, Search: "alpa"));

        await _artists.Received(1).ListAsync(
            new ArtistListQuery(0, 20, ArtistSortField.Name, SortDirection.Asc, "alpa"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ListAlbumsAsync_UnknownArtist_ReturnsNull()
    {
        Page<AlbumRow>? page = await _service.ListAlbumsAsync(Guid.NewGuid(), new AlbumListRequest(0, 50));

        page.Should().BeNull();
        await _albums.DidNotReceiveWithAnyArgs().ListAsync(default!);
    }

    [Test]
    public async Task ListAlbumsAsync_KnownArtist_FiltersTheAlbumsByTheArtist()
    {
        var artistId = Guid.NewGuid();
        _artists.ExistsAsync(artistId, Arg.Any<CancellationToken>()).Returns(true);

        await _service.ListAlbumsAsync(artistId, new AlbumListRequest(0, 50, AlbumSortField.Year));

        await _albums.Received(1).ListAsync(
            new AlbumListQuery(0, 50, AlbumSortField.Year, SortDirection.Asc, ArtistId: artistId),
            Arg.Any<CancellationToken>());
    }
}
