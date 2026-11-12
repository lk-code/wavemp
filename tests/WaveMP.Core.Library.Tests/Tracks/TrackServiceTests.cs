using AwesomeAssertions;
using NSubstitute;
using WaveMP.Core.Library.Tracks;
using WaveMP.Data.Entities;
using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Library.Tests.Tracks;

[TestFixture]
public class TrackServiceTests
{
    private ITrackRepository _tracks = null!;
    private TrackService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _tracks = Substitute.For<ITrackRepository>();
        _service = new TrackService(_tracks);
    }

    [Test]
    public async Task ListAsync_WithoutSortAndSearch_SortsByAlbumArtistAscending()
    {
        var artistId = Guid.NewGuid();

        await _service.ListAsync(new TrackListRequest(10, 20, ArtistId: artistId, Genre: "  Rock  ", HasVideo: true));

        await _tracks.Received(1).ListAsync(
            new TrackListQuery(
                10,
                20,
                TrackSortField.AlbumArtist,
                SortDirection.Asc,
                ArtistId: artistId,
                Genre: "Rock",
                HasVideo: true),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ListAsync_WithSearch_SortsByRelevanceAndCleansTheText()
    {
        await _service.ListAsync(new TrackListRequest(0, 50, Search: " Closng ", Genre: "   "));

        await _tracks.Received(1).ListAsync(
            new TrackListQuery(0, 50, TrackSortField.Relevance, SortDirection.Desc, "Closng"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ListAsync_WithUserAndMyMusic_PassesBoth()
    {
        var userId = Guid.NewGuid();

        await _service.ListAsync(new TrackListRequest(0, 50, Year: 2001, UserId: userId, MyMusic: true));

        await _tracks.Received(1).ListAsync(
            new TrackListQuery(
                0,
                50,
                TrackSortField.AlbumArtist,
                SortDirection.Asc,
                Year: 2001,
                UserId: userId,
                MyMusic: true),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ListAsync_MyMusicWithoutUser_IgnoresTheFilter()
    {
        await _service.ListAsync(new TrackListRequest(0, 50, MyMusic: true));

        await _tracks.Received(1).ListAsync(
            new TrackListQuery(0, 50, TrackSortField.AlbumArtist, SortDirection.Asc),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ListAsync_ByRating_DescendsByDefault()
    {
        var userId = Guid.NewGuid();

        await _service.ListAsync(new TrackListRequest(0, 50, TrackSortField.Rating, UserId: userId));

        await _tracks.Received(1).ListAsync(
            new TrackListQuery(0, 50, TrackSortField.Rating, SortDirection.Desc, UserId: userId),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ListAsync_ByRatingWithoutUser_FallsBackToTheDefault()
    {
        await _service.ListAsync(new TrackListRequest(0, 50, TrackSortField.Rating));

        await _tracks.Received(1).ListAsync(
            new TrackListQuery(0, 50, TrackSortField.AlbumArtist, SortDirection.Asc),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetAsync_WithUser_FindsTheTrackOfTheUser()
    {
        var userId = Guid.NewGuid();
        var trackId = Guid.NewGuid();

        await _service.GetAsync(trackId, userId);

        await _tracks.Received(1).FindAsync(trackId, userId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ListGenresAndYears_WithMyMusic_PassUserAndFilter()
    {
        var userId = Guid.NewGuid();

        await _service.ListGenresAsync(new GenreListRequest(0, 10, UserId: userId, MyMusic: true));
        await _service.ListYearsAsync(new YearListRequest(0, 10, UserId: userId, MyMusic: true));

        await _tracks.Received(1).ListGenresAsync(
            new GenreListQuery(0, 10, GenreSortField.Name, SortDirection.Asc, UserId: userId, MyMusic: true),
            Arg.Any<CancellationToken>());
        await _tracks.Received(1).ListYearsAsync(
            new YearListQuery(0, 10, SortDirection.Desc, userId, true),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ListAsync_RecentlyAdded_KeepsTheRequestedSortAndDirection()
    {
        await _service.ListAsync(new TrackListRequest(0, 50, TrackSortField.FirstSeenAt, SortDirection.Asc));

        await _tracks.Received(1).ListAsync(
            new TrackListQuery(0, 50, TrackSortField.FirstSeenAt, SortDirection.Asc),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ListFilesAsync_UnknownTrack_ReturnsNull()
    {
        _tracks.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        Page<MediaFile>? page = await _service.ListFilesAsync(Guid.NewGuid(), new TrackFileListRequest(0, 50));

        page.Should().BeNull();
        await _tracks.DidNotReceiveWithAnyArgs().ListFilesAsync(default!);
    }

    [Test]
    public async Task ListFilesAsync_KnownTrack_ListsByKindAscending()
    {
        var trackId = Guid.NewGuid();
        Page<MediaFile> files = new([], 0, 50, 0);
        _tracks.ExistsAsync(trackId, Arg.Any<CancellationToken>()).Returns(true);
        _tracks.ListFilesAsync(Arg.Any<TrackFileListQuery>(), Arg.Any<CancellationToken>()).Returns(files);

        Page<MediaFile>? page = await _service.ListFilesAsync(
            trackId,
            new TrackFileListRequest(0, 50, Kind: MediaFileKind.Video));

        page.Should().BeSameAs(files);
        await _tracks.Received(1).ListFilesAsync(
            new TrackFileListQuery(trackId, 0, 50, MediaFileSortField.Kind, SortDirection.Asc, MediaFileKind.Video),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ListGenresAndYears_WithoutSort_UseTheDefaults()
    {
        await _service.ListGenresAsync(new GenreListRequest(0, 50, GenreSortField.TrackCount, Search: " ro "));
        await _service.ListYearsAsync(new YearListRequest(0, 50));

        await _tracks.Received(1).ListGenresAsync(
            new GenreListQuery(0, 50, GenreSortField.TrackCount, SortDirection.Desc, "ro"),
            Arg.Any<CancellationToken>());
        await _tracks.Received(1).ListYearsAsync(
            new YearListQuery(0, 50, SortDirection.Desc),
            Arg.Any<CancellationToken>());
    }
}
