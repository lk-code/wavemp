using AwesomeAssertions;
using NSubstitute;
using WaveMP.Core.Library.Searches;
using WaveMP.Data.Projections;
using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Library.Tests.Searches;

[TestFixture]
public class SearchServiceTests
{
    private IArtistRepository _artists = null!;
    private IAlbumRepository _albums = null!;
    private ITrackRepository _tracks = null!;
    private SearchService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _artists = Substitute.For<IArtistRepository>();
        _albums = Substitute.For<IAlbumRepository>();
        _tracks = Substitute.For<ITrackRepository>();
        _service = new SearchService(_artists, _albums, _tracks);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public async Task SearchAsync_TextWithoutContent_ReturnsNullAndSearchesNothing(string? text)
    {
        SearchResult? result = await _service.SearchAsync(text, 0, 10);

        result.Should().BeNull();
        await _tracks.DidNotReceiveWithAnyArgs().ListAsync(default!);
    }

    [Test]
    public async Task SearchAsync_Text_SearchesArtistsAlbumsAndTracksByRelevance()
    {
        Page<ArtistRow> artists = new([], 5, 20, 0);
        Page<AlbumRow> albums = new([], 5, 20, 1);
        Page<TrackRow> tracks = new([], 5, 20, 2);
        _artists.ListAsync(Arg.Any<ArtistListQuery>(), Arg.Any<CancellationToken>()).Returns(artists);
        _albums.ListAsync(Arg.Any<AlbumListQuery>(), Arg.Any<CancellationToken>()).Returns(albums);
        _tracks.ListAsync(Arg.Any<TrackListQuery>(), Arg.Any<CancellationToken>()).Returns(tracks);

        SearchResult? result = await _service.SearchAsync("  alpa   band ", 5, 20);

        result.Should().Be(new SearchResult(artists, albums, tracks));
        await _artists.Received(1).ListAsync(
            new ArtistListQuery(5, 20, ArtistSortField.Relevance, SortDirection.Desc, "alpa band"),
            Arg.Any<CancellationToken>());
        await _albums.Received(1).ListAsync(
            new AlbumListQuery(5, 20, AlbumSortField.Relevance, SortDirection.Desc, "alpa band"),
            Arg.Any<CancellationToken>());
        await _tracks.Received(1).ListAsync(
            new TrackListQuery(5, 20, TrackSortField.Relevance, SortDirection.Desc, "alpa band"),
            Arg.Any<CancellationToken>());
    }
}
