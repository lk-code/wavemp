using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using WaveMP.Service.Dtos;
using WaveMP.Service.Tests.IntegrationTests.Extensions;
using WaveMP.Testing.Files;
using WaveMP.Testing.Media;

namespace WaveMP.Service.Tests.IntegrationTests;

/// <summary>
/// Browses and searches a library of synthetic media through the API. The media is created with ffmpeg and scanned
/// once before the tests.
/// </summary>
[TestFixture]
[Category("Integration")]
public class LibraryApiTests
{
    private TemporaryDirectory _media = null!;
    private WaveMpWebApplicationFactory _factory = null!;
    private HttpClient _anonymous = null!;
    private HttpClient _admin = null!;
    private HttpClient _user = null!;
    private IReadOnlyList<TrackDto> _tracks = null!;

    private TrackDto Opening => Track("Opening");

    private TrackDto Closing => Track("Closing");

    private TrackDto Ballad => Track("Ballad");

    private TrackDto Clip => Track("Clip");

    private TrackDto Song => Track("Song");

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _media = new TemporaryDirectory();
        TestTags first = new()
        {
            Artist = "Alpha Band",
            Album = "First Album",
            Date = "2001",
            Genre = "Rock",
        };
        await TestMediaGenerator.CreateAudioAsync(
            _media.Combine("Alpha Band/First Album/01 - Opening.mp3"),
            AudioFormat.Mp3,
            first with { Title = "Opening", Track = "1" });
        await TestMediaGenerator.CreateAudioAsync(
            _media.Combine("Alpha Band/First Album/02 - Closing.flac"),
            AudioFormat.Flac,
            first with { Title = "Closing", Track = "2" });
        await TestMediaGenerator.CreateVideoAsync(
            _media.Combine("Alpha Band/First Album/02 - Closing.mkv"),
            VideoFormat.Mkv,
            320,
            240);
        await TestMediaGenerator.CreateImageAsync(_media.Combine("Alpha Band/First Album/cover.jpg"), 300, 300);
        string embedded = _media.Combine("ballad-cover.png");
        await TestMediaGenerator.CreateImageAsync(embedded, 120, 120);
        await TestMediaGenerator.CreateAudioAsync(
            _media.Combine("Beta Singer/Second Album/01 - Ballad.m4a"),
            AudioFormat.M4a,
            new TestTags
            {
                Title = "Ballad",
                Artist = "Beta Singer",
                Album = "Second Album",
                Track = "1",
                Date = "2010",
                Genre = "Pop",
            },
            embeddedCoverPath: embedded);
        File.Delete(embedded);
        await TestMediaGenerator.CreateVideoAsync(_media.Combine("Beta Singer/Clip.mp4"), VideoFormat.Mp4, 320, 240);
        await TestMediaGenerator.CreateAudioAsync(_media.Combine("Loose - Song.mp3"), AudioFormat.Mp3);

        _factory = new WaveMpWebApplicationFactory(
            WaveMpWebApplicationFactory.TestDatabaseName + "_library",
            freshDatabase: true,
            mediaPath: _media.Path);
        _anonymous = _factory.CreateClient();
        _admin = await _factory.CreateAdminClientAsync();
        UserDto user = await _admin.CreateUserAsync(new { name = $"Listener {HttpClientExtensions.Unique()}" });
        CreatedSessionDto session = await _anonymous.CreateSessionAsync(user.Id);
        _user = _factory.CreateAuthorizedClient(session.Token);

        HttpResponseMessage response = await _admin.PostAsync("/api/v1/scans", null);
        ScanDto queued = (await response.Content.ReadFromJsonAsync<ScanDto>(HttpClientExtensions.JsonOptions))!;
        ScanDto scan = await _admin.WaitForScanAsync(queued.Id);
        scan.Status.Should().Be(JobRunStatusDto.Succeeded);

        _tracks = (await _user.GetJsonAsync<PageDto<TrackDto>>("/api/v1/tracks?limit=100")).Items;
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        _user?.Dispose();
        _admin?.Dispose();
        _anonymous?.Dispose();
        await _factory.DeleteDatabaseAsync();
        await _factory.DisposeAsync();
        _media.Dispose();
    }

    private static IEnumerable<TestCaseData> Endpoints()
    {
        string id = Guid.NewGuid().ToString();
        yield return new TestCaseData("/api/v1/artists").SetArgDisplayNames("ListArtists");
        yield return new TestCaseData($"/api/v1/artists/{id}").SetArgDisplayNames("GetArtist");
        yield return new TestCaseData($"/api/v1/artists/{id}/albums").SetArgDisplayNames("ListArtistAlbums");
        yield return new TestCaseData("/api/v1/albums").SetArgDisplayNames("ListAlbums");
        yield return new TestCaseData($"/api/v1/albums/{id}").SetArgDisplayNames("GetAlbum");
        yield return new TestCaseData($"/api/v1/albums/{id}/tracks").SetArgDisplayNames("ListAlbumTracks");
        yield return new TestCaseData($"/api/v1/albums/{id}/cover").SetArgDisplayNames("GetAlbumCover");
        yield return new TestCaseData("/api/v1/tracks").SetArgDisplayNames("ListTracks");
        yield return new TestCaseData($"/api/v1/tracks/{id}").SetArgDisplayNames("GetTrack");
        yield return new TestCaseData($"/api/v1/tracks/{id}/cover").SetArgDisplayNames("GetTrackCover");
        yield return new TestCaseData($"/api/v1/tracks/{id}/files").SetArgDisplayNames("ListTrackFiles");
        yield return new TestCaseData("/api/v1/genres").SetArgDisplayNames("ListGenres");
        yield return new TestCaseData("/api/v1/years").SetArgDisplayNames("ListYears");
        yield return new TestCaseData("/api/v1/search?q=band").SetArgDisplayNames("SearchLibrary");
    }

    [TestCaseSource(nameof(Endpoints))]
    public async Task LibraryEndpoints_WithoutToken_ReturnUnauthorized(string path)
    {
        HttpResponseMessage response = await _anonymous.GetAsync(path);

        await response.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, "auth.unauthorized");
    }

    [Test]
    public async Task ListTrackFiles_AsUser_ReturnsForbidden()
    {
        HttpResponseMessage response = await _user.GetAsync($"/api/v1/tracks/{Closing.Id}/files");

        await response.ShouldBeProblemAsync(HttpStatusCode.Forbidden, "auth.forbidden");
    }

    [Test]
    public void ListTracks_ByDefault_SortsByAlbumArtistAlbumAndNumber()
    {
        _tracks.Select(track => track.Title).Should().Equal("Opening", "Closing", "Ballad", "Clip", "Song");
    }

    [Test]
    public async Task ListTracks_WithPagingAndSorting_ReturnsTheRequestedPage()
    {
        PageDto<TrackDto> page = await _user.GetJsonAsync<PageDto<TrackDto>>("/api/v1/tracks?limit=2&offset=2");
        PageDto<TrackDto> byTitle = await _user.GetJsonAsync<PageDto<TrackDto>>(
            "/api/v1/tracks?sort=Title&direction=Desc");

        page.Should().BeEquivalentTo(new { Offset = 2, Limit = 2, Total = 5 });
        page.Items.Select(track => track.Title).Should().Equal("Ballad", "Clip");
        byTitle.Items.Select(track => track.Title).Should().Equal("Song", "Opening", "Closing", "Clip", "Ballad");
    }

    [Test]
    public async Task ListTracks_WithFilters_ReturnsTheMatchingTracks()
    {
        (await TitlesAsync($"/api/v1/tracks?artistId={Opening.Artist!.Id}")).Should().Equal("Opening", "Closing");
        (await TitlesAsync($"/api/v1/tracks?albumId={Ballad.Album!.Id}")).Should().Equal("Ballad");
        (await TitlesAsync("/api/v1/tracks?genre=rock")).Should().Equal("Opening", "Closing");
        (await TitlesAsync("/api/v1/tracks?year=2010")).Should().Equal("Ballad");
        (await TitlesAsync("/api/v1/tracks?hasVideo=true")).Should().Equal("Closing", "Clip");
        (await TitlesAsync("/api/v1/tracks?hasAudio=false")).Should().Equal("Clip");
        (await TitlesAsync("/api/v1/tracks?q=Closng")).Should().StartWith("Closing");
    }

    [TestCase("sort=Foo")]
    [TestCase("sort=42")]
    [TestCase("direction=Up")]
    [TestCase("limit=0")]
    [TestCase("limit=101")]
    [TestCase("offset=-1")]
    [TestCase("year=0")]
    [TestCase("artistId=abc")]
    [TestCase("hasVideo=maybe")]
    public async Task ListTracks_WithInvalidQuery_ReturnsBadRequest(string query)
    {
        HttpResponseMessage response = await _user.GetAsync($"/api/v1/tracks?{query}");

        await response.ShouldBeValidationProblemAsync();
    }

    [Test]
    public async Task ListTracks_WithTooLongSearch_ReturnsBadRequest()
    {
        HttpResponseMessage response = await _user.GetAsync($"/api/v1/tracks?q={new string('a', 201)}");

        (await response.ShouldBeValidationProblemAsync()).Should().ContainKey("q");
    }

    [Test]
    public void GetTrack_Values_DescribeAudioVideoCoverArtistAndAlbum()
    {
        Closing.Should().BeEquivalentTo(new
        {
            Title = "Closing",
            Artist = new { Name = "Alpha Band" },
            Album = new { Title = "First Album", Artist = new { Name = "Alpha Band" }, IsCompilation = false },
            TrackNumber = 2,
            Year = 2001,
            Genre = "Rock",
            HasAudio = true,
            HasVideo = true,
            HasCover = true,
        });
        Closing.DurationMilliseconds.Should().BeInRange(800, 1200);
        Clip.Should().BeEquivalentTo(new { HasAudio = false, HasVideo = true, HasCover = false, Album = (object?)null });
        Song.Should().BeEquivalentTo(new { Artist = new { Name = "Loose" }, HasCover = false });
        Ballad.HasCover.Should().BeTrue();
    }

    [Test]
    public async Task GetTrack_KnownAndUnknownId_ReturnsTheTrackOrNotFound()
    {
        TrackDto track = await _user.GetJsonAsync<TrackDto>($"/api/v1/tracks/{Ballad.Id}");
        HttpResponseMessage missing = await _user.GetAsync($"/api/v1/tracks/{Guid.NewGuid()}");

        track.Should().BeEquivalentTo(Ballad);
        await missing.ShouldBeProblemAsync(HttpStatusCode.NotFound, "track.not_found");
    }

    [Test]
    public async Task ListAlbums_Always_SummarizesTheirTracks()
    {
        PageDto<AlbumDto> albums = await _user.GetJsonAsync<PageDto<AlbumDto>>("/api/v1/albums");
        AlbumDto album = await _user.GetJsonAsync<AlbumDto>($"/api/v1/albums/{Opening.Album!.Id}");
        HttpResponseMessage missing = await _user.GetAsync($"/api/v1/albums/{Guid.NewGuid()}");

        albums.Items.Select(item => item.Title).Should().Equal("First Album", "Second Album");
        album.Should().BeEquivalentTo(new
        {
            Title = "First Album",
            Artist = new { Name = "Alpha Band" },
            IsCompilation = false,
            Year = 2001,
            Genre = "Rock",
            TrackCount = 2,
            HasCover = true,
        });
        album.DurationMilliseconds.Should().BeInRange(1600, 2400);
        await missing.ShouldBeProblemAsync(HttpStatusCode.NotFound, "album.not_found");
    }

    [Test]
    public async Task ListAlbumTracks_KnownAndUnknownAlbum_ListsByNumberOrReturnsNotFound()
    {
        PageDto<TrackDto> tracks = await _user.GetJsonAsync<PageDto<TrackDto>>(
            $"/api/v1/albums/{Opening.Album!.Id}/tracks");
        HttpResponseMessage missing = await _user.GetAsync($"/api/v1/albums/{Guid.NewGuid()}/tracks");

        tracks.Items.Select(track => track.Title).Should().Equal("Opening", "Closing");
        await missing.ShouldBeProblemAsync(HttpStatusCode.NotFound, "album.not_found");
    }

    [Test]
    public async Task ListArtists_Always_CountsAlbumsAndTracks()
    {
        PageDto<ArtistDto> artists = await _user.GetJsonAsync<PageDto<ArtistDto>>("/api/v1/artists");
        ArtistDto beta = await _user.GetJsonAsync<ArtistDto>($"/api/v1/artists/{Ballad.Artist!.Id}");
        PageDto<AlbumDto> albums = await _user.GetJsonAsync<PageDto<AlbumDto>>(
            $"/api/v1/artists/{Ballad.Artist.Id}/albums");
        HttpResponseMessage missing = await _user.GetAsync($"/api/v1/artists/{Guid.NewGuid()}");
        HttpResponseMessage missingAlbums = await _user.GetAsync($"/api/v1/artists/{Guid.NewGuid()}/albums");

        artists.Items.Select(artist => artist.Name).Should().Equal("Alpha Band", "Beta Singer", "Loose");
        beta.Should().BeEquivalentTo(new { Name = "Beta Singer", AlbumCount = 1, TrackCount = 2 });
        albums.Items.Select(album => album.Title).Should().Equal("Second Album");
        await missing.ShouldBeProblemAsync(HttpStatusCode.NotFound, "artist.not_found");
        await missingAlbums.ShouldBeProblemAsync(HttpStatusCode.NotFound, "artist.not_found");
    }

    [Test]
    public async Task ListGenresAndYears_Always_GroupTheTracks()
    {
        PageDto<GenreDto> genres = await _user.GetJsonAsync<PageDto<GenreDto>>("/api/v1/genres");
        PageDto<YearDto> years = await _user.GetJsonAsync<PageDto<YearDto>>("/api/v1/years");

        genres.Items.Should().BeEquivalentTo(
            [
                new GenreDto { Name = "Pop", TrackCount = 1, AlbumCount = 1 },
                new GenreDto { Name = "Rock", TrackCount = 2, AlbumCount = 1 },
            ],
            options => options.WithStrictOrdering());
        years.Items.Select(year => year.Year).Should().Equal(2010, 2001);
    }

    [Test]
    public async Task SearchLibrary_WithTypos_FindsArtistsAlbumsAndTracks()
    {
        SearchResultDto artist = await _user.GetJsonAsync<SearchResultDto>("/api/v1/search?q=alpa%20band");
        SearchResultDto track = await _user.GetJsonAsync<SearchResultDto>("/api/v1/search?q=balad&limit=5");

        artist.Artists.Items.First().Name.Should().Be("Alpha Band");
        artist.Albums.Items.Should().Contain(album => album.Title == "First Album");
        track.Tracks.Items.First().Title.Should().Be("Ballad");
        track.Tracks.Limit.Should().Be(5);
    }

    [TestCase("")]
    [TestCase("q=%20")]
    [TestCase("q=band&limit=51")]
    public async Task SearchLibrary_WithInvalidQuery_ReturnsBadRequest(string query)
    {
        HttpResponseMessage response = await _user.GetAsync($"/api/v1/search?{query}");

        await response.ShouldBeValidationProblemAsync();
    }

    [Test]
    public async Task GetAlbumCover_AlbumWithPicture_DeliversTheOriginalFile()
    {
        HttpResponseMessage response = await _user.GetAsync($"/api/v1/albums/{Opening.Album!.Id}/cover");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("image/jpeg");
        (await response.Content.ReadAsByteArrayAsync()).Should().Equal(
            await File.ReadAllBytesAsync(_media.Combine("Alpha Band/First Album/cover.jpg")));
    }

    [Test]
    public async Task GetTrackCover_TrackOfAnAlbumWithPicture_DeliversTheAlbumCover()
    {
        HttpResponseMessage response = await _user.GetAsync($"/api/v1/tracks/{Opening.Id}/cover");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsByteArrayAsync()).Should().Equal(
            await File.ReadAllBytesAsync(_media.Combine("Alpha Band/First Album/cover.jpg")));
    }

    [Test]
    public async Task GetAlbumCover_AlbumWithEmbeddedPictureOnly_DeliversThePictureOfItsTrack()
    {
        HttpResponseMessage album = await _user.GetAsync($"/api/v1/albums/{Ballad.Album!.Id}/cover");
        HttpResponseMessage track = await _user.GetAsync($"/api/v1/tracks/{Ballad.Id}/cover");

        album.StatusCode.Should().Be(HttpStatusCode.OK);
        album.Content.Headers.ContentType!.MediaType.Should().Be("image/png");
        (await album.Content.ReadAsByteArrayAsync()).Should().StartWith([0x89, 0x50, 0x4E, 0x47]);
        track.Content.Headers.ContentType!.MediaType.Should().Be("image/png");
    }

    [Test]
    public async Task GetCover_WithoutCoverOrOwner_ReturnsNotFoundWithTheCode()
    {
        await (await _user.GetAsync($"/api/v1/tracks/{Song.Id}/cover"))
            .ShouldBeProblemAsync(HttpStatusCode.NotFound, "cover.not_found");
        await (await _user.GetAsync($"/api/v1/tracks/{Clip.Id}/cover"))
            .ShouldBeProblemAsync(HttpStatusCode.NotFound, "cover.not_found");
        await (await _user.GetAsync($"/api/v1/tracks/{Guid.NewGuid()}/cover"))
            .ShouldBeProblemAsync(HttpStatusCode.NotFound, "track.not_found");
        await (await _user.GetAsync($"/api/v1/albums/{Guid.NewGuid()}/cover"))
            .ShouldBeProblemAsync(HttpStatusCode.NotFound, "album.not_found");
    }

    [Test]
    public async Task ListTrackFiles_AsAdmin_ListsAudioBeforeVideo()
    {
        PageDto<MediaFileDto> files = await _admin.GetJsonAsync<PageDto<MediaFileDto>>(
            $"/api/v1/tracks/{Closing.Id}/files");
        PageDto<MediaFileDto> videos = await _admin.GetJsonAsync<PageDto<MediaFileDto>>(
            $"/api/v1/tracks/{Closing.Id}/files?kind=Video");
        HttpResponseMessage missing = await _admin.GetAsync($"/api/v1/tracks/{Guid.NewGuid()}/files");

        files.Items.Select(file => (file.FileName, file.Kind, file.IsPrimary, file.Issue)).Should().Equal(
            ("02 - Closing.flac", MediaFileKindDto.Audio, true, (ScanIssueCodeDto?)null),
            ("02 - Closing.mkv", MediaFileKindDto.Video, true, (ScanIssueCodeDto?)null));
        files.Items[0].Should().BeEquivalentTo(new
        {
            RelativePath = "Alpha Band/First Album/02 - Closing.flac",
            Extension = "flac",
            Codec = "flac",
            IsLossless = true,
            IsWritable = true,
        });
        files.Items[1].Should().BeEquivalentTo(new { Width = 320, Height = 240, IsWritable = false });
        videos.Items.Select(file => file.FileName).Should().Equal("02 - Closing.mkv");
        await missing.ShouldBeProblemAsync(HttpStatusCode.NotFound, "track.not_found");
    }

    [TestCase("kind=Picture")]
    [TestCase("sort=9")]
    public async Task ListTrackFiles_WithInvalidQuery_ReturnsBadRequest(string query)
    {
        HttpResponseMessage response = await _admin.GetAsync($"/api/v1/tracks/{Closing.Id}/files?{query}");

        await response.ShouldBeValidationProblemAsync();
    }

    private TrackDto Track(string title)
    {
        return _tracks.Single(track => track.Title == title);
    }

    private async Task<IReadOnlyList<string>> TitlesAsync(string path)
    {
        return [.. (await _user.GetJsonAsync<PageDto<TrackDto>>(path)).Items.Select(track => track.Title)];
    }
}
