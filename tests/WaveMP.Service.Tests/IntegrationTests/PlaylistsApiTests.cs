using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AwesomeAssertions;
using WaveMP.Service.Dtos;
using WaveMP.Service.Tests.IntegrationTests.Extensions;
using WaveMP.Testing.Files;
using WaveMP.Testing.Media;

namespace WaveMP.Service.Tests.IntegrationTests;

/// <summary>
/// Playlists of profiles through the API: the playlists, their entries, their covers and the list of recently played
/// tracks, with a small library of synthetic media.
/// </summary>
[TestFixture]
[Category("Integration")]
public class PlaylistsApiTests
{
    private const string Path = "/api/v1/me/playlists";

    private TemporaryDirectory _media = null!;
    private WaveMpWebApplicationFactory _factory = null!;
    private HttpClient _anonymous = null!;
    private HttpClient _admin = null!;
    private IReadOnlyList<TrackDto> _tracks = null!;
    private byte[] _picture = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _media = new TemporaryDirectory();
        await CreateTrackAsync("Alpha", "One", "First", withCover: true);
        await CreateTrackAsync("Alpha", "One", "Second", withCover: false);
        await CreateTrackAsync("Beta", "Two", "Third", withCover: true);
        await CreateTrackAsync("Gamma", "Three", "Fourth", withCover: false);
        using (TemporaryDirectory upload = new())
        {
            await TestMediaGenerator.CreateImageAsync(upload.Combine("upload.png"), 400, 250);
            _picture = await File.ReadAllBytesAsync(upload.Combine("upload.png"));
        }

        _factory = new WaveMpWebApplicationFactory(
            WaveMpWebApplicationFactory.TestDatabaseName + "_playlists",
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
    public async Task Playlists_WithoutToken_ReturnUnauthorized()
    {
        HttpResponseMessage listed = await _anonymous.GetAsync(Path);
        HttpResponseMessage created = await _anonymous.PostAsJsonAsync(Path, new { name = "Mix" });
        HttpResponseMessage entries = await _anonymous.GetAsync($"{Path}/{Guid.NewGuid()}/tracks");
        HttpResponseMessage cover = await _anonymous.GetAsync($"{Path}/{Guid.NewGuid()}/cover");
        HttpResponseMessage recent = await _anonymous.GetAsync("/api/v1/me/recently-played");

        await listed.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, "auth.unauthorized");
        await created.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, "auth.unauthorized");
        await entries.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, "auth.unauthorized");
        await cover.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, "auth.unauthorized");
        await recent.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, "auth.unauthorized");
    }

    [Test]
    public async Task CreateMyPlaylist_WithTracks_CreatesItWithTheCoversOfDifferentAlbums()
    {
        using HttpClient client = await CreateProfileAsync();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            Path,
            new { name = "  Road   Trip ", trackIds = new[] { Track("Second"), Track("First"), Track("Fourth"), Track("Third") } });

        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        PlaylistDto playlist =
            (await response.Content.ReadFromJsonAsync<PlaylistDto>(HttpClientExtensions.JsonOptions))!;
        response.Headers.Location!.AbsolutePath.Should().Be($"{Path}/{playlist.Id}");
        playlist.Name.Should().Be("Road Trip");
        playlist.TrackCount.Should().Be(4);
        playlist.DurationMilliseconds.Should().BeCloseTo(_tracks.Sum(track => track.DurationMilliseconds ?? 0), 4);
        playlist.Cover.Kind.Should().Be(PlaylistCoverKindDto.Tracks);
        playlist.Cover.Version.Should().BeNull();
        // One cover per album: Second and First share album One, Fourth has none.
        playlist.Cover.TrackIds.Should().Equal(Track("Second"), Track("Third"));
        (await client.GetJsonAsync<PlaylistDto>($"{Path}/{playlist.Id}")).Should().BeEquivalentTo(playlist);
    }

    [Test]
    public async Task CreateMyPlaylist_WithoutTracks_HasNoCover()
    {
        using HttpClient client = await CreateProfileAsync();

        PlaylistDto playlist = await CreatePlaylistAsync(client, "Empty");

        playlist.TrackCount.Should().Be(0);
        playlist.DurationMilliseconds.Should().Be(0);
        playlist.Cover.Kind.Should().Be(PlaylistCoverKindDto.None);
        playlist.Cover.TrackIds.Should().BeEmpty();
    }

    [TestCase("{\"name\":\"Bell\\u0007\"}", "name", ValidationErrorCodeDto.InvalidValue)]
    [TestCase("{\"name\":\"   \"}", "name", ValidationErrorCodeDto.Required)]
    [TestCase("{}", "name", ValidationErrorCodeDto.Required)]
    public async Task CreateMyPlaylist_InvalidName_ReturnsTheField(string body, string field, ValidationErrorCodeDto code)
    {
        using HttpClient client = await CreateProfileAsync();

        HttpResponseMessage response = await client.SendJsonAsync("POST", Path, body);

        IReadOnlyList<ValidationErrorDto> errors = await response.ShouldBeValidationProblemAsync();
        errors.Should().ContainSingle().Which.Should().BeEquivalentTo(new { Field = field, Code = code });
    }

    [Test]
    public async Task CreateMyPlaylist_UnknownTrack_ReturnsNotFound()
    {
        using HttpClient client = await CreateProfileAsync();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            Path,
            new { name = "Mix", trackIds = new[] { Track("First"), Guid.NewGuid() } });

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "track.not_found");
        (await client.GetJsonAsync<PageDto<PlaylistDto>>(Path)).Total.Should().Be(0);
    }

    [Test]
    public async Task ListMyPlaylists_Always_ListsOnlyTheOwnPlaylistsSortedAndSearched()
    {
        using HttpClient client = await CreateProfileAsync();
        using HttpClient other = await CreateProfileAsync();
        PlaylistDto rock = await CreatePlaylistAsync(client, "rock classics");
        PlaylistDto chill = await CreatePlaylistAsync(client, "Chill");
        PlaylistDto road = await CreatePlaylistAsync(client, "Road Rock");
        await CreatePlaylistAsync(other, "Rock of another profile");

        PageDto<PlaylistDto> byName = await client.GetJsonAsync<PageDto<PlaylistDto>>(Path);
        PageDto<PlaylistDto> newest =
            await client.GetJsonAsync<PageDto<PlaylistDto>>($"{Path}?sort=CreatedAt&direction=Desc&limit=1");
        PageDto<PlaylistDto> searched = await client.GetJsonAsync<PageDto<PlaylistDto>>($"{Path}?q=ROCK");

        byName.Items.Select(playlist => playlist.Id).Should().Equal(chill.Id, road.Id, rock.Id);
        newest.Total.Should().Be(3);
        newest.Items.Should().ContainSingle().Which.Id.Should().Be(road.Id);
        searched.Items.Select(playlist => playlist.Id).Should().Equal(road.Id, rock.Id);
    }

    [TestCase("sort=Unknown")]
    [TestCase("direction=Sideways")]
    [TestCase("limit=0")]
    public async Task ListMyPlaylists_InvalidQuery_ReturnsBadRequest(string query)
    {
        using HttpClient client = await CreateProfileAsync();

        HttpResponseMessage response = await client.GetAsync($"{Path}?{query}");

        await response.ShouldBeValidationProblemAsync();
    }

    [Test]
    public async Task Playlist_OfAnotherProfile_IsNotFound()
    {
        using HttpClient client = await CreateProfileAsync();
        using HttpClient other = await CreateProfileAsync();
        PlaylistDto playlist = await CreatePlaylistAsync(other, "Private", Track("First"));
        string path = $"{Path}/{playlist.Id}";

        HttpResponseMessage read = await client.GetAsync(path);
        HttpResponseMessage renamed = await client.PatchAsJsonAsync(path, new { name = "Mine" });
        HttpResponseMessage deleted = await client.DeleteAsync(path);
        HttpResponseMessage entries = await client.GetAsync($"{path}/tracks");
        HttpResponseMessage added = await client.PostAsJsonAsync($"{path}/tracks", new { trackIds = new[] { Track("First") } });
        HttpResponseMessage cover = await client.PutAsync($"{path}/cover", Picture(_picture));

        await read.ShouldBeProblemAsync(HttpStatusCode.NotFound, "playlist.not_found");
        await renamed.ShouldBeProblemAsync(HttpStatusCode.NotFound, "playlist.not_found");
        await deleted.ShouldBeProblemAsync(HttpStatusCode.NotFound, "playlist.not_found");
        await entries.ShouldBeProblemAsync(HttpStatusCode.NotFound, "playlist.not_found");
        await added.ShouldBeProblemAsync(HttpStatusCode.NotFound, "playlist.not_found");
        await cover.ShouldBeProblemAsync(HttpStatusCode.NotFound, "playlist.not_found");
        (await other.GetJsonAsync<PlaylistDto>(path)).TrackCount.Should().Be(1);
    }

    [Test]
    public async Task UpdateMyPlaylist_WithName_RenamesIt()
    {
        using HttpClient client = await CreateProfileAsync();
        PlaylistDto playlist = await CreatePlaylistAsync(client, "Old");

        HttpResponseMessage response = await client.PatchAsJsonAsync($"{Path}/{playlist.Id}", new { name = " New " });
        HttpResponseMessage unchanged = await client.PatchAsJsonAsync($"{Path}/{playlist.Id}", new { });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        PlaylistDto renamed = (await response.Content.ReadFromJsonAsync<PlaylistDto>(HttpClientExtensions.JsonOptions))!;
        renamed.Name.Should().Be("New");
        renamed.UpdatedAt.Should().BeOnOrAfter(playlist.UpdatedAt);
        unchanged.StatusCode.Should().Be(HttpStatusCode.OK);
        (await unchanged.Content.ReadFromJsonAsync<PlaylistDto>(HttpClientExtensions.JsonOptions))!.Name
            .Should().Be("New");
    }

    [Test]
    public async Task DeleteMyPlaylist_Existing_DeletesItWithItsCover()
    {
        using HttpClient client = await CreateProfileAsync();
        PlaylistDto playlist = await CreatePlaylistAsync(client, "Gone", Track("First"));
        (await client.PutAsync($"{Path}/{playlist.Id}/cover", Picture(_picture))).StatusCode
            .Should().Be(HttpStatusCode.OK);
        string coverFolder = System.IO.Path.Combine(_factory.DataPath, "playlists", playlist.Id.ToString("D"));
        Directory.Exists(coverFolder).Should().BeTrue();

        HttpResponseMessage response = await client.DeleteAsync($"{Path}/{playlist.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await (await client.GetAsync($"{Path}/{playlist.Id}"))
            .ShouldBeProblemAsync(HttpStatusCode.NotFound, "playlist.not_found");
        Directory.Exists(coverFolder).Should().BeFalse();
        (await client.GetJsonAsync<TrackDto>($"/api/v1/tracks/{Track("First")}")).Title.Should().Be("First");
    }

    [Test]
    public async Task AddMyPlaylistEntries_DuplicatesAndPosition_InsertsThemInOrder()
    {
        using HttpClient client = await CreateProfileAsync();
        PlaylistDto playlist = await CreatePlaylistAsync(client, "Mix", Track("First"), Track("Second"));
        string path = $"{Path}/{playlist.Id}/tracks";

        HttpResponseMessage response = await client.PostAsJsonAsync(
            path,
            new { trackIds = new[] { Track("Third"), Track("First") }, position = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        AddedPlaylistEntriesDto added =
            (await response.Content.ReadFromJsonAsync<AddedPlaylistEntriesDto>(HttpClientExtensions.JsonOptions))!;
        response.Headers.Location!.AbsolutePath.Should().Be($"{path}/{added.Entries[0].Id}");
        added.Entries.Select(entry => (entry.Position, entry.Track.Title)).Should().Equal((1, "Third"), (2, "First"));
        added.Playlist.TrackCount.Should().Be(4);
        PageDto<PlaylistEntryDto> entries = await client.GetJsonAsync<PageDto<PlaylistEntryDto>>(path);
        entries.Items.Select(entry => entry.Track.Title).Should().Equal("First", "Third", "First", "Second");
        entries.Items.Select(entry => entry.Position).Should().Equal(0, 1, 2, 3);
        entries.Items.Select(entry => entry.Id).Should().OnlyHaveUniqueItems();
        (await client.GetJsonAsync<PlaylistEntryDto>($"{path}/{added.Entries[1].Id}")).Position.Should().Be(2);
    }

    [Test]
    public async Task AddMyPlaylistEntries_PositionAfterTheEnd_ReturnsTheLargestPosition()
    {
        using HttpClient client = await CreateProfileAsync();
        PlaylistDto playlist = await CreatePlaylistAsync(client, "Mix", Track("First"), Track("Second"));

        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"{Path}/{playlist.Id}/tracks",
            new { trackIds = new[] { Track("Third") }, position = 3 });

        IReadOnlyList<ValidationErrorDto> errors = await response.ShouldBeValidationProblemAsync();
        errors.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new { Field = "position", Code = ValidationErrorCodeDto.OutOfRange, Min = 0, Max = 2 });
    }

    [TestCase("{}", ValidationErrorCodeDto.Required)]
    [TestCase("{\"trackIds\":[]}", ValidationErrorCodeDto.MinItems)]
    public async Task AddMyPlaylistEntries_WithoutTracks_ReturnsTheField(string body, ValidationErrorCodeDto code)
    {
        using HttpClient client = await CreateProfileAsync();
        PlaylistDto playlist = await CreatePlaylistAsync(client, "Mix");

        HttpResponseMessage response = await client.SendJsonAsync("POST", $"{Path}/{playlist.Id}/tracks", body);

        IReadOnlyList<ValidationErrorDto> errors = await response.ShouldBeValidationProblemAsync();
        errors.Should().ContainSingle().Which.Should().BeEquivalentTo(new { Field = "trackIds", Code = code });
    }

    [Test]
    public async Task UpdateMyPlaylistEntry_NewPosition_MovesTheEntry()
    {
        using HttpClient client = await CreateProfileAsync();
        PlaylistDto playlist = await CreatePlaylistAsync(
            client,
            "Mix",
            Track("First"),
            Track("Second"),
            Track("Third"));
        string path = $"{Path}/{playlist.Id}/tracks";
        IReadOnlyList<PlaylistEntryDto> before = (await client.GetJsonAsync<PageDto<PlaylistEntryDto>>(path)).Items;

        HttpResponseMessage response = await client.PatchAsJsonAsync($"{path}/{before[0].Id}", new { position = 2 });
        HttpResponseMessage outside = await client.PatchAsJsonAsync($"{path}/{before[0].Id}", new { position = 3 });
        HttpResponseMessage missing = await client.SendJsonAsync("PATCH", $"{path}/{before[0].Id}", "{}");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        PlaylistEntryDto moved =
            (await response.Content.ReadFromJsonAsync<PlaylistEntryDto>(HttpClientExtensions.JsonOptions))!;
        moved.Id.Should().Be(before[0].Id);
        moved.Position.Should().Be(2);
        (await client.GetJsonAsync<PageDto<PlaylistEntryDto>>(path)).Items.Select(entry => entry.Track.Title)
            .Should().Equal("Second", "Third", "First");
        (await outside.ShouldBeValidationProblemAsync()).Should().ContainSingle().Which.Should().BeEquivalentTo(
            new { Field = "position", Code = ValidationErrorCodeDto.OutOfRange, Min = 0, Max = 2 });
        (await missing.ShouldBeValidationProblemAsync()).Should().ContainSingle().Which.Should().BeEquivalentTo(
            new { Field = "position", Code = ValidationErrorCodeDto.Required });
    }

    [Test]
    public async Task DeleteMyPlaylistEntry_Existing_RemovesOnlyThisEntry()
    {
        using HttpClient client = await CreateProfileAsync();
        PlaylistDto playlist = await CreatePlaylistAsync(
            client,
            "Mix",
            Track("First"),
            Track("Second"),
            Track("First"));
        string path = $"{Path}/{playlist.Id}/tracks";
        IReadOnlyList<PlaylistEntryDto> before = (await client.GetJsonAsync<PageDto<PlaylistEntryDto>>(path)).Items;

        HttpResponseMessage response = await client.DeleteAsync($"{path}/{before[0].Id}");
        HttpResponseMessage again = await client.DeleteAsync($"{path}/{before[0].Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await again.ShouldBeProblemAsync(HttpStatusCode.NotFound, "playlist.entry_not_found");
        IReadOnlyList<PlaylistEntryDto> after = (await client.GetJsonAsync<PageDto<PlaylistEntryDto>>(path)).Items;
        after.Select(entry => (entry.Id, entry.Position)).Should().Equal((before[1].Id, 0), (before[2].Id, 1));
        await (await client.GetAsync($"{path}/{before[0].Id}"))
            .ShouldBeProblemAsync(HttpStatusCode.NotFound, "playlist.entry_not_found");
    }

    [Test]
    public async Task SetMyPlaylistCover_Picture_StoresASquareWebPUntilItIsRemoved()
    {
        using HttpClient client = await CreateProfileAsync();
        PlaylistDto playlist = await CreatePlaylistAsync(client, "Covered", Track("First"));
        string path = $"{Path}/{playlist.Id}/cover";

        HttpResponseMessage response = await client.PutAsync(path, Picture(_picture));

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        PlaylistDto covered = (await response.Content.ReadFromJsonAsync<PlaylistDto>(HttpClientExtensions.JsonOptions))!;
        covered.Cover.Kind.Should().Be(PlaylistCoverKindDto.Custom);
        covered.Cover.Version.Should().NotBeNullOrEmpty();
        covered.Cover.TrackIds.Should().BeEmpty();

        HttpResponseMessage picture = await client.GetAsync($"{path}?size=96");
        picture.StatusCode.Should().Be(HttpStatusCode.OK);
        picture.Content.Headers.ContentType!.MediaType.Should().Be("image/webp");
        picture.Headers.CacheControl!.Private.Should().BeTrue();
        picture.Headers.CacheControl.NoCache.Should().BeTrue();
        byte[] webp = await picture.Content.ReadAsByteArrayAsync();
        System.Text.Encoding.ASCII.GetString(webp, 8, 4).Should().Be("WEBP");
        using HttpRequestMessage again = new(HttpMethod.Get, $"{path}?size=96");
        again.Headers.IfNoneMatch.Add(picture.Headers.ETag!);
        (await client.SendAsync(again)).StatusCode.Should().Be(HttpStatusCode.NotModified);

        HttpResponseMessage removed = await client.DeleteAsync(path);
        removed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.GetJsonAsync<PlaylistDto>($"{Path}/{playlist.Id}")).Cover.Kind
            .Should().Be(PlaylistCoverKindDto.Tracks);
        await (await client.GetAsync(path)).ShouldBeProblemAsync(HttpStatusCode.NotFound, "cover.not_found");
    }

    [Test]
    public async Task SetMyPlaylistCover_NewPicture_ChangesTheVersion()
    {
        using HttpClient client = await CreateProfileAsync();
        PlaylistDto playlist = await CreatePlaylistAsync(client, "Covered");
        string path = $"{Path}/{playlist.Id}/cover";

        PlaylistDto first = await SetCoverAsync(client, path);
        PlaylistDto second = await SetCoverAsync(client, path);

        second.Cover.Version.Should().NotBe(first.Cover.Version);
    }

    [Test]
    public async Task SetMyPlaylistCover_NotAPicture_ReturnsUnsupportedFormat()
    {
        using HttpClient client = await CreateProfileAsync();
        PlaylistDto playlist = await CreatePlaylistAsync(client, "Covered");

        HttpResponseMessage text = await client.PutAsync(
            $"{Path}/{playlist.Id}/cover",
            new ByteArrayContent("just some text"u8.ToArray()));
        HttpResponseMessage empty = await client.PutAsync($"{Path}/{playlist.Id}/cover", new ByteArrayContent([]));

        await text.ShouldBeProblemAsync(HttpStatusCode.UnsupportedMediaType, "cover.unsupported_format");
        await empty.ShouldBeProblemAsync(HttpStatusCode.UnsupportedMediaType, "cover.unsupported_format");
    }

    [Test]
    public async Task SetMyPlaylistCover_DamagedPicture_ReturnsUnreadable()
    {
        using HttpClient client = await CreateProfileAsync();
        PlaylistDto playlist = await CreatePlaylistAsync(client, "Covered");
        byte[] damaged = [.. _picture.Take(64), .. new byte[2048]];

        HttpResponseMessage response = await client.PutAsync($"{Path}/{playlist.Id}/cover", Picture(damaged));

        await response.ShouldBeProblemAsync(HttpStatusCode.UnprocessableEntity, "cover.unreadable");
        (await client.GetJsonAsync<PlaylistDto>($"{Path}/{playlist.Id}")).Cover.Kind
            .Should().Be(PlaylistCoverKindDto.None);
    }

    [Test]
    public async Task SetMyPlaylistCover_TooLarge_ReturnsTooLarge()
    {
        using HttpClient client = await CreateProfileAsync();
        PlaylistDto playlist = await CreatePlaylistAsync(client, "Covered");
        byte[] large = [.. _picture, .. new byte[10 * 1024 * 1024]];

        HttpResponseMessage response = await client.PutAsync($"{Path}/{playlist.Id}/cover", Picture(large));

        await response.ShouldBeProblemAsync(HttpStatusCode.RequestEntityTooLarge, "cover.too_large");
    }

    [Test]
    public async Task ListMyRecentlyPlayedTracks_AfterPlays_ListsEveryTrackOnceNewestFirst()
    {
        using HttpClient client = await CreateProfileAsync();
        using HttpClient other = await CreateProfileAsync();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        await PlayAsync(client, Track("First"), now.AddMinutes(-30));
        await PlayAsync(client, Track("Second"), now.AddMinutes(-20));
        await PlayAsync(client, Track("First"), now.AddMinutes(-10));
        await PlayAsync(other, Track("Third"), now.AddMinutes(-5));

        PageDto<RecentlyPlayedTrackDto> page =
            await client.GetJsonAsync<PageDto<RecentlyPlayedTrackDto>>("/api/v1/me/recently-played");

        page.Total.Should().Be(2);
        page.Items.Select(item => item.Track.Title).Should().Equal("First", "Second");
        page.Items[0].LastPlayedAt.Should().BeCloseTo(now.AddMinutes(-10), TimeSpan.FromSeconds(1));
    }

    private Guid Track(string title)
    {
        return _tracks.Single(track => track.Title == title).Id;
    }

    private async Task CreateTrackAsync(string artist, string album, string title, bool withCover)
    {
        if (withCover)
        {
            await TestMediaGenerator.CreateImageAsync(_media.Combine($"{artist}/{album}/cover.jpg"), 300, 300);
        }

        await TestMediaGenerator.CreateAudioAsync(
            _media.Combine($"{artist}/{album}/{title}.mp3"),
            AudioFormat.Mp3,
            new TestTags { Title = title, Artist = artist, Album = album });
    }

    private async Task<HttpClient> CreateProfileAsync()
    {
        UserDto user = await _admin.CreateUserAsync(new { name = $"Playlists {HttpClientExtensions.Unique()}" });
        CreatedSessionDto session = await _anonymous.CreateSessionAsync(user.Id);
        return _factory.CreateAuthorizedClient(session.Token);
    }

    private static async Task<PlaylistDto> CreatePlaylistAsync(HttpClient client, string name, params Guid[] trackIds)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(Path, new { name, trackIds });
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<PlaylistDto>(HttpClientExtensions.JsonOptions))!;
    }

    private async Task<PlaylistDto> SetCoverAsync(HttpClient client, string path)
    {
        HttpResponseMessage response = await client.PutAsync(path, Picture(_picture));
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<PlaylistDto>(HttpClientExtensions.JsonOptions))!;
    }

    private static async Task PlayAsync(HttpClient client, Guid trackId, DateTimeOffset playedAt)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/me/history", new { trackId, playedAt });
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
    }

    private static ByteArrayContent Picture(byte[] content)
    {
        ByteArrayContent body = new(content);
        body.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        return body;
    }
}
