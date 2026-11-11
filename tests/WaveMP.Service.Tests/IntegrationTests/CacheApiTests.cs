using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using WaveMP.Service.Dtos;
using WaveMP.Service.Tests.IntegrationTests.Extensions;
using WaveMP.Testing.Files;
using WaveMP.Testing.Media;

namespace WaveMP.Service.Tests.IntegrationTests;

/// <summary>
/// The administration of the cache: its usage and clearing it.
/// </summary>
[TestFixture]
[Category("Integration")]
public class CacheApiTests
{
    private TemporaryDirectory _media = null!;
    private WaveMpWebApplicationFactory _factory = null!;
    private HttpClient _anonymous = null!;
    private HttpClient _admin = null!;
    private HttpClient _user = null!;
    private Guid _albumId;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _media = new TemporaryDirectory();
        await TestMediaGenerator.CreateImageAsync(_media.Combine("Band/Album/cover.jpg"), 640, 480);
        await TestMediaGenerator.CreateAudioAsync(
            _media.Combine("Band/Album/01 - Song.mp3"),
            AudioFormat.Mp3,
            new TestTags { Title = "Song", Artist = "Band", Album = "Album", Track = "1" });

        _factory = new WaveMpWebApplicationFactory(
            WaveMpWebApplicationFactory.TestDatabaseName + "_cache",
            freshDatabase: true,
            mediaPath: _media.Path);
        _anonymous = _factory.CreateClient();
        _admin = await _factory.CreateAdminClientAsync();
        UserDto user = await _admin.CreateUserAsync(new { name = $"Listener {HttpClientExtensions.Unique()}" });
        _user = _factory.CreateAuthorizedClient((await _anonymous.CreateSessionAsync(user.Id)).Token);

        HttpResponseMessage response = await _admin.PostAsync("/api/v1/scans", null);
        ScanDto queued = (await response.Content.ReadFromJsonAsync<ScanDto>(HttpClientExtensions.JsonOptions))!;
        (await _admin.WaitForScanAsync(queued.Id)).Status.Should().Be(JobRunStatusDto.Succeeded);
        _albumId = (await _admin.GetJsonAsync<PageDto<AlbumDto>>("/api/v1/albums")).Items.Single().Id;
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
        yield return new TestCaseData("GET", "/api/v1/cache").SetArgDisplayNames("GetCache");
        yield return new TestCaseData("DELETE", "/api/v1/cache").SetArgDisplayNames("ClearCache");
        yield return new TestCaseData("GET", "/api/v1/settings/cache").SetArgDisplayNames("GetCacheSettings");
        yield return new TestCaseData("PATCH", "/api/v1/settings/cache").SetArgDisplayNames("UpdateCacheSettings");
    }

    [TestCaseSource(nameof(Endpoints))]
    public async Task CacheEndpoints_WithoutToken_ReturnUnauthorized(string method, string path)
    {
        HttpResponseMessage response = await _anonymous.SendJsonAsync(method, path, method == "PATCH" ? "{}" : null);

        await response.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, "auth.unauthorized");
    }

    [TestCaseSource(nameof(Endpoints))]
    public async Task CacheEndpoints_AsUser_ReturnForbidden(string method, string path)
    {
        HttpResponseMessage response = await _user.SendJsonAsync(method, path, method == "PATCH" ? "{}" : null);

        await response.ShouldBeProblemAsync(HttpStatusCode.Forbidden, "auth.forbidden");
    }

    [Test]
    public async Task GetCache_WithScaledCovers_CountsThem()
    {
        await _user.GetAsync($"/api/v1/albums/{_albumId}/cover?size=96");
        await _user.GetAsync($"/api/v1/albums/{_albumId}/cover?size=300");

        CacheUsageDto usage = await _admin.GetJsonAsync<CacheUsageDto>("/api/v1/cache");

        usage.IsAvailable.Should().BeTrue();
        usage.MaxSizeBytes.Should().Be(2_000_000_000);
        usage.Covers.EntryCount.Should().BeGreaterThanOrEqualTo(2);
        usage.Covers.SizeBytes.Should().BePositive();
        usage.Transcodes.Should().BeEquivalentTo(new CacheAreaUsageDto { SizeBytes = 0, EntryCount = 0 });
        usage.SizeBytes.Should().Be(usage.Covers.SizeBytes + usage.Transcodes.SizeBytes);
        usage.EntryCount.Should().Be(usage.Covers.EntryCount + usage.Transcodes.EntryCount);
    }

    [Test]
    public async Task ClearCache_AsAdmin_EmptiesTheCacheAndCoversAreCreatedAgain()
    {
        (await _user.GetAsync($"/api/v1/albums/{_albumId}/cover?size=1000")).StatusCode.Should().Be(HttpStatusCode.OK);
        int before = (await _admin.GetJsonAsync<CacheUsageDto>("/api/v1/cache")).Covers.EntryCount;

        HttpResponseMessage cleared = await _admin.DeleteAsync("/api/v1/cache");
        CacheUsageDto empty = await _admin.GetJsonAsync<CacheUsageDto>("/api/v1/cache");
        string[] remaining = [.. Directory.EnumerateFiles(
            Path.Combine(_factory.DataPath, "cache", "covers"),
            "*",
            SearchOption.AllDirectories)];
        HttpResponseMessage again = await _user.GetAsync($"/api/v1/albums/{_albumId}/cover?size=96");
        CacheUsageDto refilled = await _admin.GetJsonAsync<CacheUsageDto>("/api/v1/cache");

        before.Should().BeGreaterThanOrEqualTo(3);
        cleared.StatusCode.Should().Be(HttpStatusCode.NoContent);
        empty.Should().BeEquivalentTo(new { SizeBytes = 0L, EntryCount = 0 });
        remaining.Should().BeEmpty();
        again.Content.Headers.ContentType!.MediaType.Should().Be("image/webp");
        refilled.Covers.EntryCount.Should().Be(1);
    }
}
