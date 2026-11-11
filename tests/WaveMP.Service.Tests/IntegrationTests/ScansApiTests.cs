using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WaveMP.Core.Jobs.Definitions;
using WaveMP.Core.Jobs.Runs;
using WaveMP.Data;
using WaveMP.Data.Entities;
using WaveMP.Service.Dtos;
using WaveMP.Service.Tests.IntegrationTests.Extensions;
using WaveMP.Testing.Files;
using WaveMP.Testing.Media;

namespace WaveMP.Service.Tests.IntegrationTests;

/// <summary>
/// Scans synthetic media through the API: the media is created with ffmpeg before the service starts.
/// </summary>
[TestFixture]
[Category("Integration")]
public class ScansApiTests
{
    private TemporaryDirectory _media = null!;
    private WaveMpWebApplicationFactory _factory = null!;
    private HttpClient _anonymous = null!;
    private HttpClient _admin = null!;
    private HttpResponseMessage _firstResponse = null!;
    private ScanDto _firstScan = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _media = new TemporaryDirectory();
        await TestMediaGenerator.CreateAudioAsync(_media.Combine("Band/Album/01 - One.mp3"), AudioFormat.Mp3);
        await TestMediaGenerator.CreateVideoAsync(_media.Combine("Band/Album/01 - One.mkv"), VideoFormat.Mkv, 320, 240);
        await TestMediaGenerator.CreateAudioAsync(_media.Combine("Band/Album/02 - Two.mp3"), AudioFormat.Mp3);
        await TestMediaGenerator.CreateAudioAsync(_media.Combine("Band/Album/02 - Two.flac"), AudioFormat.Flac);
        await TestMediaGenerator.CreateImageAsync(_media.Combine("Band/Album/cover.jpg"), 200, 200);
        await TestMediaGenerator.CreateAudioAsync(_media.Combine("Loose - Song.mp3"), AudioFormat.Mp3);

        _factory = new WaveMpWebApplicationFactory(
            WaveMpWebApplicationFactory.TestDatabaseName + "_scans",
            freshDatabase: true,
            mediaPath: _media.Path);
        _anonymous = _factory.CreateClient();
        _admin = await _factory.CreateAdminClientAsync();

        _firstResponse = await _admin.PostAsync("/api/v1/scans", null);
        ScanDto queued = (await _firstResponse.Content.ReadFromJsonAsync<ScanDto>(HttpClientExtensions.JsonOptions))!;
        _firstScan = await _admin.WaitForScanAsync(queued.Id);
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        _admin?.Dispose();
        _anonymous?.Dispose();
        await _factory.DeleteDatabaseAsync();
        await _factory.DisposeAsync();
        _media.Dispose();
    }

    private static IEnumerable<TestCaseData> Endpoints()
    {
        string id = Guid.NewGuid().ToString();
        yield return new TestCaseData("POST", "/api/v1/scans").SetArgDisplayNames("StartScan");
        yield return new TestCaseData("GET", "/api/v1/scans").SetArgDisplayNames("ListScans");
        yield return new TestCaseData("GET", "/api/v1/scans/current").SetArgDisplayNames("GetCurrentScan");
        yield return new TestCaseData("GET", $"/api/v1/scans/{id}").SetArgDisplayNames("GetScan");
    }

    [TestCaseSource(nameof(Endpoints))]
    public async Task ScanEndpoints_WithoutToken_ReturnUnauthorized(string method, string path)
    {
        HttpResponseMessage response = await _anonymous.SendJsonAsync(method, path);

        await response.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, "auth.unauthorized");
    }

    [TestCaseSource(nameof(Endpoints))]
    public async Task ScanEndpoints_AsUser_ReturnForbidden(string method, string path)
    {
        UserDto user = await _admin.CreateUserAsync(new { name = $"User {HttpClientExtensions.Unique()}" });
        CreatedSessionDto session = await _anonymous.CreateSessionAsync(user.Id);
        using HttpClient client = _factory.CreateAuthorizedClient(session.Token);

        HttpResponseMessage response = await client.SendJsonAsync(method, path);

        await response.ShouldBeProblemAsync(HttpStatusCode.Forbidden, "auth.forbidden");
    }

    [Test]
    public void StartScan_AsAdmin_IsAcceptedWithTheLocationOfTheScan()
    {
        _firstResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        _firstResponse.Headers.Location!.AbsolutePath.Should().Be($"/api/v1/scans/{_firstScan.Id}");
        _firstScan.Trigger.Should().Be(JobTriggerDto.Manual);
    }

    [Test]
    public void FirstScan_WithMedia_FindsTheTracksAndFinishes()
    {
        _firstScan.Status.Should().Be(JobRunStatusDto.Succeeded);
        _firstScan.ErrorCode.Should().BeNull();
        _firstScan.StartedAt.Should().NotBeNull();
        _firstScan.FinishedAt.Should().NotBeNull();
        _firstScan.DurationMilliseconds.Should().BeGreaterThanOrEqualTo(0);
        _firstScan.IsPartial.Should().BeFalse();
        _firstScan.Progress.Should().BeNull();
        _firstScan.Issues.Should().BeEmpty();
        _firstScan.Statistics.Should().BeEquivalentTo(new
        {
            FilesFound = 6,
            FilesAdded = 6,
            TracksAdded = 3,
            TracksTotal = 3,
            AlbumsTotal = 1,
            ArtistsTotal = 2,
        });
    }

    [Test]
    public async Task FirstScan_WithMedia_StoresGroupsPrimaryFilesAlbumsAndCovers()
    {
        await using AsyncServiceScope scope = _factory.Services.CreateAsyncScope();
        WaveMpDbContext db = scope.ServiceProvider.GetRequiredService<WaveMpDbContext>();

        List<Track> tracks = await db.Tracks
            .Include(track => track.Files)
            .Include(track => track.Album).ThenInclude(album => album!.Covers)
            .Include(track => track.Artist)
            .ToListAsync();

        Track one = tracks.Single(track => track.BaseName == "01 - One");
        one.HasAudio.Should().BeTrue();
        one.HasVideo.Should().BeTrue();
        one.Title.Should().Be("One");
        one.Artist!.Name.Should().Be("Band");
        one.FirstSeenAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(5));
        one.Album!.Covers.Should().ContainSingle(cover => cover.IsPrimary);
        Track two = tracks.Single(track => track.BaseName == "02 - Two");
        two.Files.Single(file => file.IsPrimary).Extension.Should().Be("flac");
        two.Files.Should().HaveCount(2);
        Track loose = tracks.Single(track => track.BaseName == "Loose - Song");
        (loose.Title, loose.Artist!.Name, loose.AlbumId).Should().Be(("Song", "Loose", null));
    }

    [Test]
    public async Task Rescan_WithoutChanges_ReadsNothingAgain()
    {
        ScanDto scan = await StartAndWaitAsync();

        scan.Status.Should().Be(JobRunStatusDto.Succeeded);
        scan.Statistics!.FilesUnchanged.Should().Be(6);
        scan.Statistics.FilesAdded.Should().Be(0);
        scan.Statistics.TracksTotal.Should().Be(3);
    }

    [Test]
    public async Task StartScan_WhileAScanIsActive_ReturnsConflict()
    {
        IJobRunTracker tracker = _factory.Services.GetRequiredService<IJobRunTracker>();
        var blocking = Guid.NewGuid();
        tracker.TryReserve(JobIds.LibraryScan, blocking).Should().BeTrue();
        try
        {
            HttpResponseMessage response = await _admin.PostAsync("/api/v1/scans", null);

            await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "scan.already_running");
        }
        finally
        {
            tracker.Release(JobIds.LibraryScan, blocking);
        }
    }

    [Test]
    public async Task GetCurrentScan_WithoutActiveScan_ReturnsNotRunning()
    {
        HttpResponseMessage response = await _admin.GetAsync("/api/v1/scans/current");

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "scan.not_running");
    }

    [Test]
    public async Task GetScan_WithUnknownId_ReturnsNotFound()
    {
        HttpResponseMessage response = await _admin.GetAsync($"/api/v1/scans/{Guid.NewGuid()}");

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "scan.not_found");
    }

    [Test]
    public async Task ListScans_WithPagingSortingAndFilters_ReturnsTheMatchingScans()
    {
        await StartAndWaitAsync();

        PageDto<ScanDto> newest = await _admin.GetJsonAsync<PageDto<ScanDto>>("/api/v1/scans?limit=1");
        PageDto<ScanDto> oldest = await _admin.GetJsonAsync<PageDto<ScanDto>>("/api/v1/scans?limit=1&direction=Asc");
        PageDto<ScanDto> succeeded = await _admin.GetJsonAsync<PageDto<ScanDto>>(
            "/api/v1/scans?status=Succeeded&trigger=Manual&limit=100");
        PageDto<ScanDto> watcher = await _admin.GetJsonAsync<PageDto<ScanDto>>("/api/v1/scans?trigger=FileWatcher");

        newest.Total.Should().BeGreaterThanOrEqualTo(2);
        newest.Items.Should().ContainSingle();
        oldest.Items.Single().Id.Should().Be(_firstScan.Id);
        newest.Items.Single().QueuedAt.Should().BeAfter(oldest.Items.Single().QueuedAt);
        succeeded.Items.Should().HaveCount(succeeded.Total).And.OnlyContain(scan =>
            scan.Status == JobRunStatusDto.Succeeded && scan.Statistics != null);
        watcher.Total.Should().Be(0);
    }

    [TestCase("status=Done")]
    [TestCase("status=99")]
    [TestCase("trigger=9")]
    [TestCase("direction=Up")]
    [TestCase("limit=0")]
    public async Task ListScans_WithInvalidQuery_ReturnsBadRequest(string query)
    {
        HttpResponseMessage response = await _admin.GetAsync($"/api/v1/scans?{query}");

        await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "request.invalid");
    }

    private async Task<ScanDto> StartAndWaitAsync()
    {
        HttpResponseMessage response = await _admin.PostAsync("/api/v1/scans", null);
        response.StatusCode.Should().Be(HttpStatusCode.Accepted, await response.Content.ReadAsStringAsync());
        ScanDto queued = (await response.Content.ReadFromJsonAsync<ScanDto>(HttpClientExtensions.JsonOptions))!;
        queued.Status.Should().BeOneOf(JobRunStatusDto.Queued, JobRunStatusDto.Running);
        return await _admin.WaitForScanAsync(queued.Id);
    }
}
