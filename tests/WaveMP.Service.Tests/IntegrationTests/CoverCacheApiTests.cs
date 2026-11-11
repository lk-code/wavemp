using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using WaveMP.Service.Dtos;
using WaveMP.Service.Tests.IntegrationTests.Extensions;
using WaveMP.Testing.Files;
using WaveMP.Testing.Media;

namespace WaveMP.Service.Tests.IntegrationTests;

/// <summary>
/// Scaled covers through the API: sizes, HTTP caching, the preparation after a scan and the invalidation after a
/// change of the picture. The media is created with ffmpeg and scanned once before the tests.
/// </summary>
[TestFixture]
[Category("Integration")]
public class CoverCacheApiTests
{
    private TemporaryDirectory _media = null!;
    private WaveMpWebApplicationFactory _factory = null!;
    private HttpClient _admin = null!;
    private HttpClient _user = null!;
    private Dictionary<string, Guid> _albums = null!;
    private string[] _preparedFiles = null!;

    private string CoverFolder => Path.Combine(_factory.DataPath, "cache", "covers");

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _media = new TemporaryDirectory();
        await CreateAlbumAsync("Alpha", "Big Album", "cover.jpg", 1200, 800);
        await CreateAlbumAsync("Beta", "Small Album", "cover.png", 200, 100);
        await CreateAlbumAsync("Gamma", "Change Album", "cover.jpg", 400, 400);

        _factory = new WaveMpWebApplicationFactory(
            WaveMpWebApplicationFactory.TestDatabaseName + "_covers",
            freshDatabase: true,
            mediaPath: _media.Path);
        _admin = await _factory.CreateAdminClientAsync();
        using HttpClient anonymous = _factory.CreateClient();
        UserDto user = await _admin.CreateUserAsync(new { name = $"Viewer {HttpClientExtensions.Unique()}" });
        _user = _factory.CreateAuthorizedClient((await anonymous.CreateSessionAsync(user.Id)).Token);

        // The test changes a picture itself and scans afterwards; the file watcher must not scan in between.
        (await _admin.PatchAsJsonAsync("/api/v1/settings/library", new { fileWatcherEnabled = false }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await ScanAsync()).Status.Should().Be(JobRunStatusDto.Succeeded);
        _preparedFiles = [.. Directory.EnumerateFiles(CoverFolder, "*", SearchOption.AllDirectories)
            .Select(Path.GetFileName)
            .OfType<string>()];

        PageDto<AlbumDto> albums = await _user.GetJsonAsync<PageDto<AlbumDto>>("/api/v1/albums?limit=100");
        _albums = albums.Items.ToDictionary(album => album.Title, album => album.Id);
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        _user?.Dispose();
        _admin?.Dispose();
        await _factory.DeleteDatabaseAsync();
        await _factory.DisposeAsync();
        _media.Dispose();
    }

    [TestCase(96, 96, 64)]
    [TestCase(300, 300, 200)]
    [TestCase(1000, 1000, 667)]
    [TestCase(150, 300, 200)]
    [TestCase(2000, 1000, 667)]
    public async Task GetAlbumCover_WithSize_DeliversTheRoundedSizeAsWebP(int size, int width, int height)
    {
        HttpResponseMessage response = await _user.GetAsync($"/api/v1/albums/{_albums["Big Album"]}/cover?size={size}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("image/webp");
        response.Headers.ETag!.Tag.Should().EndWith($"-{CoverSizeOf(size)}\"");
        (await ImageProbe.GetSizeAsync(await response.Content.ReadAsByteArrayAsync())).Should().Be((width, height));
    }

    [Test]
    public async Task GetTrackCover_SmallPicture_IsNeverEnlarged()
    {
        TrackDto track = await TrackAsync("Small Album");

        HttpResponseMessage response = await _user.GetAsync($"/api/v1/tracks/{track.Id}/cover?size=1000");

        response.Content.Headers.ContentType!.MediaType.Should().Be("image/webp");
        (await ImageProbe.GetSizeAsync(await response.Content.ReadAsByteArrayAsync())).Should().Be((200, 100));
    }

    [Test]
    public async Task GetAlbumCover_Always_SendsTheHeadersOfHttpCaching()
    {
        HttpResponseMessage response = await _user.GetAsync($"/api/v1/albums/{_albums["Big Album"]}/cover?size=96");

        response.Headers.ETag!.IsWeak.Should().BeFalse();
        response.Content.Headers.LastModified.Should().NotBeNull();
        response.Headers.CacheControl!.Private.Should().BeTrue();
        response.Headers.CacheControl.NoCache.Should().BeTrue();
    }

    [Test]
    public async Task GetAlbumCover_WithTheCurrentETag_ReturnsNotModified()
    {
        string path = $"/api/v1/albums/{_albums["Big Album"]}/cover?size=300";
        HttpResponseMessage first = await _user.GetAsync(path);
        using HttpRequestMessage request = new(HttpMethod.Get, path);
        request.Headers.IfNoneMatch.Add(first.Headers.ETag!);

        HttpResponseMessage second = await _user.SendAsync(request);

        second.StatusCode.Should().Be(HttpStatusCode.NotModified);
        (await second.Content.ReadAsByteArrayAsync()).Should().BeEmpty();
        second.Headers.ETag.Should().Be(first.Headers.ETag);
    }

    [Test]
    public async Task GetAlbumCover_WithAnotherETag_DeliversThePicture()
    {
        using HttpRequestMessage request = new(HttpMethod.Get, $"/api/v1/albums/{_albums["Big Album"]}/cover?size=96");
        request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue("\"outdated\""));

        HttpResponseMessage response = await _user.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [TestCase("0")]
    [TestCase("4097")]
    [TestCase("-5")]
    [TestCase("abc")]
    public async Task GetCover_InvalidSize_ReturnsBadRequest(string size)
    {
        HttpResponseMessage album = await _user.GetAsync($"/api/v1/albums/{_albums["Big Album"]}/cover?size={size}");
        HttpResponseMessage track = await _user.GetAsync($"/api/v1/tracks/{Guid.NewGuid()}/cover?size={size}");

        JsonObject errors = await album.ShouldBeValidationProblemAsync();
        errors.Should().ContainKey("size");
        await track.ShouldBeValidationProblemAsync();
    }

    [Test]
    public async Task GetAlbumCover_WithoutSize_DeliversTheOriginalWithAnETag()
    {
        HttpResponseMessage response = await _user.GetAsync($"/api/v1/albums/{_albums["Big Album"]}/cover");

        response.Content.Headers.ContentType!.MediaType.Should().Be("image/jpeg");
        response.Headers.ETag!.Tag.Should().EndWith("-original\"");
        (await response.Content.ReadAsByteArrayAsync()).Should().Equal(
            await File.ReadAllBytesAsync(_media.Combine("Alpha/Big Album/cover.jpg")));
    }

    [Test]
    public async Task Scan_Always_PreparesTheSmallerSizesOfEveryCover()
    {
        PageDto<JobRunDto> runs = await _admin.GetJsonAsync<PageDto<JobRunDto>>(
            "/api/v1/jobs/cover-cache/runs?trigger=LibraryScan&limit=100");

        runs.Items.Should().Contain(run => run.Status == JobRunStatusDto.Succeeded);
        _preparedFiles.Count(file => file.EndsWith("-96.webp", StringComparison.Ordinal)).Should().Be(3);
        _preparedFiles.Count(file => file.EndsWith("-300.webp", StringComparison.Ordinal)).Should().Be(3);
        _preparedFiles.Should().HaveCount(6);
    }

    [Test]
    public async Task GetAlbumCover_AfterTheSourceChanged_DeliversTheNewPictureAndRemovesTheOld()
    {
        string path = $"/api/v1/albums/{_albums["Change Album"]}/cover?size=96";
        HttpResponseMessage before = await _user.GetAsync(path);
        string oldFile = CacheFileOf(before.Headers.ETag!);
        File.Exists(oldFile).Should().BeTrue();

        await TestMediaGenerator.CreateImageAsync(_media.Combine("Gamma/Change Album/cover.jpg"), 800, 400);
        (await ScanAsync()).Status.Should().Be(JobRunStatusDto.Succeeded);
        HttpResponseMessage after = await _user.GetAsync(path);

        after.Headers.ETag.Should().NotBe(before.Headers.ETag);
        (await ImageProbe.GetSizeAsync(await after.Content.ReadAsByteArrayAsync())).Should().Be((96, 48));
        File.Exists(oldFile).Should().BeFalse();
        File.Exists(CacheFileOf(after.Headers.ETag!)).Should().BeTrue();
    }

    private async Task CreateAlbumAsync(string artist, string album, string picture, int width, int height)
    {
        await TestMediaGenerator.CreateImageAsync(_media.Combine($"{artist}/{album}/{picture}"), width, height);
        await TestMediaGenerator.CreateAudioAsync(
            _media.Combine($"{artist}/{album}/01 - {album} Song.mp3"),
            AudioFormat.Mp3,
            new TestTags { Title = $"{album} Song", Artist = artist, Album = album, Track = "1" });
    }

    private async Task<ScanDto> ScanAsync()
    {
        HttpResponseMessage response = await _admin.PostAsync("/api/v1/scans", null);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted, await response.Content.ReadAsStringAsync());
        ScanDto queued = (await response.Content.ReadFromJsonAsync<ScanDto>(HttpClientExtensions.JsonOptions))!;
        return await _admin.WaitForScanAsync(queued.Id);
    }

    private async Task<TrackDto> TrackAsync(string album)
    {
        PageDto<TrackDto> tracks = await _user.GetJsonAsync<PageDto<TrackDto>>(
            $"/api/v1/albums/{_albums[album]}/tracks");
        return tracks.Items.Single();
    }

    /// <summary>
    /// The file of the cache that a scaled picture comes from: its ETag is the prefix and the size.
    /// </summary>
    private string CacheFileOf(EntityTagHeaderValue etag)
    {
        string name = etag.Tag.Trim('"');
        return Path.Combine(CoverFolder, name[30..32], name + ".webp");
    }

    private static int CoverSizeOf(int requested)
    {
        return requested <= 96 ? 96 : requested <= 300 ? 300 : 1000;
    }
}
