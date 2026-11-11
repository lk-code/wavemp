using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using WaveMP.Service.Dtos;
using WaveMP.Service.Tests.IntegrationTests.Extensions;
using WaveMP.Testing.Files;
using WaveMP.Testing.Media;

namespace WaveMP.Service.Tests.IntegrationTests;

/// <summary>
/// Records and reads the play history of profiles through the API, with a small library of synthetic media.
/// </summary>
[TestFixture]
[Category("Integration")]
public class HistoryApiTests
{
    private const string Path = "/api/v1/me/history";

    private TemporaryDirectory _media = null!;
    private WaveMpWebApplicationFactory _factory = null!;
    private HttpClient _anonymous = null!;
    private HttpClient _admin = null!;
    private IReadOnlyList<TrackDto> _tracks = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _media = new TemporaryDirectory();
        await TestMediaGenerator.CreateAudioAsync(
            _media.Combine("First.mp3"),
            AudioFormat.Mp3,
            new TestTags { Title = "First" });
        await TestMediaGenerator.CreateAudioAsync(
            _media.Combine("Second.mp3"),
            AudioFormat.Mp3,
            new TestTags { Title = "Second" });

        _factory = new WaveMpWebApplicationFactory(
            WaveMpWebApplicationFactory.TestDatabaseName + "_history",
            freshDatabase: true,
            mediaPath: _media.Path);
        _anonymous = _factory.CreateClient();
        _admin = await _factory.CreateAdminClientAsync();

        HttpResponseMessage response = await _admin.PostAsync("/api/v1/scans", null);
        ScanDto queued = (await response.Content.ReadFromJsonAsync<ScanDto>(HttpClientExtensions.JsonOptions))!;
        ScanDto scan = await _admin.WaitForScanAsync(queued.Id);
        scan.Status.Should().Be(JobRunStatusDto.Succeeded);

        _tracks = (await _admin.GetJsonAsync<PageDto<TrackDto>>("/api/v1/tracks?limit=100")).Items;
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

    [Test]
    public async Task History_WithoutToken_ReturnsUnauthorized()
    {
        HttpResponseMessage created = await _anonymous.PostAsJsonAsync(Path, new { trackId = Track("First") });
        HttpResponseMessage listed = await _anonymous.GetAsync(Path);
        HttpResponseMessage read = await _anonymous.GetAsync($"{Path}/{Guid.NewGuid()}");

        await created.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, "auth.unauthorized");
        await listed.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, "auth.unauthorized");
        await read.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, "auth.unauthorized");
    }

    [Test]
    public async Task CreateMyHistoryEntry_WithoutPlayedAt_RecordsThePlayNow()
    {
        using HttpClient client = await CreateProfileAsync();
        DateTimeOffset before = DateTimeOffset.UtcNow;

        HttpResponseMessage response = await client.PostAsJsonAsync(Path, new { trackId = Track("First") });

        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        PlayHistoryEntryDto entry =
            (await response.Content.ReadFromJsonAsync<PlayHistoryEntryDto>(HttpClientExtensions.JsonOptions))!;
        response.Headers.Location!.AbsolutePath.Should().Be($"{Path}/{entry.Id}");
        entry.Track.Id.Should().Be(Track("First"));
        entry.Track.Title.Should().Be("First");
        entry.PlayedAt.Should().BeCloseTo(before, TimeSpan.FromSeconds(30));
        (await client.GetJsonAsync<PlayHistoryEntryDto>($"{Path}/{entry.Id}")).Should().BeEquivalentTo(entry);
    }

    [Test]
    public async Task CreateMyHistoryEntry_WithPlayedAt_KeepsIt()
    {
        using HttpClient client = await CreateProfileAsync();
        DateTimeOffset playedAt = new(DateTimeOffset.UtcNow.AddHours(-3).Ticks / TimeSpan.TicksPerSecond
            * TimeSpan.TicksPerSecond, TimeSpan.Zero);

        PlayHistoryEntryDto entry = await CreateAsync(client, Track("Second"), playedAt);

        entry.PlayedAt.Should().Be(playedAt);
    }

    [TestCase(1)]
    [TestCase(-8 * 24)]
    public async Task CreateMyHistoryEntry_PlayedAtOutsideTheAllowedRange_ReturnsInvalidValue(int hours)
    {
        using HttpClient client = await CreateProfileAsync();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            Path,
            new { trackId = Track("First"), playedAt = DateTimeOffset.UtcNow.AddHours(hours) });

        IReadOnlyList<ValidationErrorDto> errors = await response.ShouldBeValidationProblemAsync();
        errors.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new { Field = "playedAt", Code = ValidationErrorCodeDto.InvalidValue });
    }

    [Test]
    public async Task CreateMyHistoryEntry_WithoutTrack_ReturnsRequired()
    {
        using HttpClient client = await CreateProfileAsync();

        HttpResponseMessage response = await client.PostAsJsonAsync(Path, new { });

        IReadOnlyList<ValidationErrorDto> errors = await response.ShouldBeValidationProblemAsync();
        errors.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new { Field = "trackId", Code = ValidationErrorCodeDto.Required });
    }

    [Test]
    public async Task CreateMyHistoryEntry_UnknownTrack_ReturnsNotFound()
    {
        using HttpClient client = await CreateProfileAsync();

        HttpResponseMessage response = await client.PostAsJsonAsync(Path, new { trackId = Guid.NewGuid() });

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "track.not_found");
    }

    [Test]
    public async Task ListMyHistory_Always_ListsOnlyTheOwnPlaysNewestFirst()
    {
        using HttpClient client = await CreateProfileAsync();
        using HttpClient other = await CreateProfileAsync();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        PlayHistoryEntryDto older = await CreateAsync(client, Track("First"), now.AddMinutes(-30));
        PlayHistoryEntryDto newer = await CreateAsync(client, Track("Second"), now.AddMinutes(-10));
        PlayHistoryEntryDto oldest = await CreateAsync(client, Track("Second"), now.AddMinutes(-50));
        await CreateAsync(other, Track("First"), now.AddMinutes(-20));

        PageDto<PlayHistoryEntryDto> page = await client.GetJsonAsync<PageDto<PlayHistoryEntryDto>>(Path);
        PageDto<PlayHistoryEntryDto> second =
            await client.GetJsonAsync<PageDto<PlayHistoryEntryDto>>($"{Path}?offset=1&limit=1");

        page.Total.Should().Be(3);
        page.Items.Select(entry => entry.Id).Should().Equal(newer.Id, older.Id, oldest.Id);
        second.Items.Should().ContainSingle().Which.Id.Should().Be(older.Id);
        second.Offset.Should().Be(1);
        second.Limit.Should().Be(1);
    }

    [Test]
    public async Task GetMyHistoryEntry_OfAnotherProfile_ReturnsNotFound()
    {
        using HttpClient client = await CreateProfileAsync();
        using HttpClient other = await CreateProfileAsync();
        PlayHistoryEntryDto entry = await CreateAsync(other, Track("First"), null);

        HttpResponseMessage response = await client.GetAsync($"{Path}/{entry.Id}");
        HttpResponseMessage unknown = await client.GetAsync($"{Path}/{Guid.NewGuid()}");

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "history.not_found");
        await unknown.ShouldBeProblemAsync(HttpStatusCode.NotFound, "history.not_found");
    }

    [TestCase("limit=0")]
    [TestCase("offset=-1")]
    public async Task ListMyHistory_InvalidPaging_ReturnsBadRequest(string query)
    {
        using HttpClient client = await CreateProfileAsync();

        HttpResponseMessage response = await client.GetAsync($"{Path}?{query}");

        await response.ShouldBeValidationProblemAsync();
    }

    private Guid Track(string title)
    {
        return _tracks.Single(track => track.Title == title).Id;
    }

    private async Task<HttpClient> CreateProfileAsync()
    {
        UserDto user = await _admin.CreateUserAsync(new { name = $"History {HttpClientExtensions.Unique()}" });
        CreatedSessionDto session = await _anonymous.CreateSessionAsync(user.Id);
        return _factory.CreateAuthorizedClient(session.Token);
    }

    private static async Task<PlayHistoryEntryDto> CreateAsync(
        HttpClient client,
        Guid trackId,
        DateTimeOffset? playedAt)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            Path,
            new { trackId, playedAt },
            HttpClientExtensions.JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<PlayHistoryEntryDto>(HttpClientExtensions.JsonOptions))!;
    }
}
