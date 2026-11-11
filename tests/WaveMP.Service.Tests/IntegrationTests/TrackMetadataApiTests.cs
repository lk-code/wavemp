using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WaveMP.Data;
using WaveMP.Data.Entities;
using WaveMP.Service.Dtos;
using WaveMP.Service.Tests.IntegrationTests.Extensions;
using WaveMP.Testing.Files;
using WaveMP.Testing.Media;

namespace WaveMP.Service.Tests.IntegrationTests;

/// <summary>
/// Edits the metadata of a library of synthetic media through the API. Every test changes only the files of its own
/// tracks; the file watcher is off, so only the API changes the library.
/// </summary>
[TestFixture]
[Category("Integration")]
public class TrackMetadataApiTests
{
    private static readonly TestTags _album = new()
    {
        Artist = "Band",
        Album = "Album",
        Date = "2001",
        Genre = "Rock",
    };

    private TemporaryDirectory _media = null!;
    private WaveMpWebApplicationFactory _factory = null!;
    private HttpClient _anonymous = null!;
    private HttpClient _admin = null!;
    private HttpClient _user = null!;
    private IReadOnlyList<TrackDto> _tracks = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _media = new TemporaryDirectory();
        await AudioAsync("Band/Album/01 - Song.mp3", _album with { Title = "Song", Track = "1/3" });
        await TestMediaGenerator.CreateAudioAsync(
            _media.Combine("Band/Album/02 - Duo.flac"),
            AudioFormat.Flac,
            _album with { Title = "Duo", Track = "2/3" });
        await TestMediaGenerator.CreateVideoAsync(
            _media.Combine("Band/Album/02 - Duo.mkv"),
            VideoFormat.Mkv,
            320,
            240,
            new TestTags { Title = "Duo" });
        await AudioAsync("Band/Album/03 - Changed.mp3", _album with { Title = "Changed", Track = "3/3" });
        await TestMediaGenerator.CreateVideoAsync(
            _media.Combine("Band/Video Only.mp4"),
            VideoFormat.Mp4,
            320,
            240,
            new TestTags { Title = "Video Only", Artist = "Band" });
        await AudioAsync("Locked.mp3", new TestTags { Title = "Locked" });
        await AudioAsync("Unlocked.mp3", new TestTags { Title = "Unlocked" });
        await AudioAsync("Keep.mp3", new TestTags { Title = "Keep", Artist = "Keeper", Genre = "Pop" });
        await AudioAsync("Genre.mp3", new TestTags { Title = "Genre Song", Genre = "Pop" });
        await AudioAsync("Empty.mp3", new TestTags { Title = "Empty" });
        await AudioAsync("Loose.mp3", null);
        File.SetAttributes(_media.Combine("Locked.mp3"), FileAttributes.ReadOnly);
        File.SetAttributes(_media.Combine("Unlocked.mp3"), FileAttributes.ReadOnly);

        _factory = new WaveMpWebApplicationFactory(
            WaveMpWebApplicationFactory.TestDatabaseName + "_track_metadata",
            freshDatabase: true,
            mediaPath: _media.Path);
        _anonymous = _factory.CreateClient();
        _admin = await _factory.CreateAdminClientAsync();
        UserDto user = await _admin.CreateUserAsync(new { name = $"Listener {HttpClientExtensions.Unique()}" });
        CreatedSessionDto session = await _anonymous.CreateSessionAsync(user.Id);
        _user = _factory.CreateAuthorizedClient(session.Token);

        HttpResponseMessage settings = await _admin.SendJsonAsync(
            "PATCH",
            "/api/v1/settings/library",
            """{ "fileWatcherEnabled": false }""");
        settings.StatusCode.Should().Be(HttpStatusCode.OK, await settings.Content.ReadAsStringAsync());

        HttpResponseMessage response = await _admin.PostAsync("/api/v1/scans", null);
        ScanDto queued = (await response.Content.ReadFromJsonAsync<ScanDto>(HttpClientExtensions.JsonOptions))!;
        ScanDto scan = await _admin.WaitForScanAsync(queued.Id);
        scan.Status.Should().Be(JobRunStatusDto.Succeeded);

        _tracks = (await _admin.GetJsonAsync<PageDto<TrackDto>>("/api/v1/tracks?limit=100")).Items;
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

    [TestCase("GET")]
    [TestCase("PATCH")]
    public async Task TrackMetadata_WithoutToken_ReturnsUnauthorized(string method)
    {
        HttpResponseMessage response = await _anonymous.SendJsonAsync(
            method,
            $"/api/v1/tracks/{Track("Song").Id}/metadata",
            method == "PATCH" ? "{}" : null);

        await response.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, "auth.unauthorized");
    }

    [TestCase("GET")]
    [TestCase("PATCH")]
    public async Task TrackMetadata_AsUser_ReturnsForbidden(string method)
    {
        HttpResponseMessage response = await _user.SendJsonAsync(
            method,
            $"/api/v1/tracks/{Track("Song").Id}/metadata",
            method == "PATCH" ? """{ "title": "Hacked" }""" : null);

        await response.ShouldBeProblemAsync(HttpStatusCode.Forbidden, "auth.forbidden");
    }

    [TestCase("GET")]
    [TestCase("PATCH")]
    public async Task TrackMetadata_UnknownTrack_ReturnsNotFound(string method)
    {
        HttpResponseMessage response = await _admin.SendJsonAsync(
            method,
            $"/api/v1/tracks/{Guid.NewGuid()}/metadata",
            method == "PATCH" ? """{ "title": "Nobody" }""" : null);

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "track.not_found");
    }

    [Test]
    public async Task GetTrackMetadata_AudioTrack_ReturnsTheTagsAndEditable()
    {
        TrackMetadataDto metadata = await GetMetadataAsync("Song");

        metadata.Should().BeEquivalentTo(new
        {
            TrackId = Track("Song").Id,
            Status = TrackMetadataStatusDto.Editable,
            AudioFilePath = "Band/Album/01 - Song.mp3",
            Title = "Song",
            Artist = "Band",
            Album = "Album",
            AlbumArtist = "Band",
            Year = 2001,
            TrackNumber = 1,
            DiscNumber = (int?)null,
            Genre = "Rock",
        });
    }

    [Test]
    public async Task GetTrackMetadata_VideoOnly_ReturnsNoAudio()
    {
        TrackMetadataDto metadata = await GetMetadataAsync("Video Only");

        metadata.Status.Should().Be(TrackMetadataStatusDto.NoAudio);
        metadata.AudioFilePath.Should().BeNull();
        metadata.Title.Should().Be("Video Only");
    }

    [Test]
    public async Task GetTrackMetadata_ReadOnlyFile_ReturnsNotWritable()
    {
        TrackMetadataDto metadata = await GetMetadataAsync("Locked");

        metadata.Status.Should().Be(TrackMetadataStatusDto.NotWritable);
        metadata.AudioFilePath.Should().Be("Locked.mp3");
    }

    [Test]
    public async Task GetTrackMetadata_FileMadeWritableAfterTheScan_ReturnsEditable()
    {
        (await GetMetadataAsync("Unlocked")).Status.Should().Be(TrackMetadataStatusDto.NotWritable);

        File.SetAttributes(_media.Combine("Unlocked.mp3"), FileAttributes.Normal);

        (await GetMetadataAsync("Unlocked")).Status.Should().Be(TrackMetadataStatusDto.Editable);
        await using AsyncServiceScope scope = _factory.Services.CreateAsyncScope();
        WaveMpDbContext db = scope.ServiceProvider.GetRequiredService<WaveMpDbContext>();
        (await db.MediaFiles.SingleAsync(file => file.RelativePath == "Unlocked.mp3")).IsWritable.Should().BeTrue();
    }

    [Test]
    public async Task UpdateTrackMetadata_VideoOnly_ReturnsConflictAndLeavesTheVideoAlone()
    {
        byte[] hash = Hash("Band/Video Only.mp4");

        HttpResponseMessage response = await PatchAsync("Video Only", """{ "title": "Changed Video" }""");

        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "track.no_audio");
        Hash("Band/Video Only.mp4").Should().Equal(hash);
    }

    [Test]
    public async Task UpdateTrackMetadata_ReadOnlyFile_ReturnsConflictAndWritesNothing()
    {
        byte[] hash = Hash("Locked.mp3");

        HttpResponseMessage response = await PatchAsync("Locked", """{ "title": "Unlocked now" }""");

        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "track.not_writable");
        Hash("Locked.mp3").Should().Equal(hash);
        (await BackupsOfAsync("Locked.mp3")).Should().BeEmpty();
    }

    [Test]
    public async Task UpdateTrackMetadata_AllFields_WritesTagsBacksUpAndReadsTheTrackAgain()
    {
        TrackDto before = Track("Changed");
        HttpResponseMessage response = await PatchAsync("Changed", """
            {
              "title": "Renamed",
              "artist": "New Artist",
              "album": "New Album",
              "albumArtist": "New Artist",
              "year": 2020,
              "trackNumber": 7,
              "discNumber": 2,
              "genre": "Jazz"
            }
            """);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        TrackMetadataDto metadata = (await response.Content.ReadFromJsonAsync<TrackMetadataDto>(
            HttpClientExtensions.JsonOptions))!;
        metadata.Should().BeEquivalentTo(new
        {
            Status = TrackMetadataStatusDto.Editable,
            Title = "Renamed",
            Artist = "New Artist",
            Album = "New Album",
            AlbumArtist = "New Artist",
            Year = 2020,
            TrackNumber = 7,
            DiscNumber = 2,
            Genre = "Jazz",
        });

        TrackDto track = await _admin.GetJsonAsync<TrackDto>($"/api/v1/tracks/{before.Id}");
        track.Title.Should().Be("Renamed");
        track.Artist!.Name.Should().Be("New Artist");
        track.Album!.Title.Should().Be("New Album");
        track.Album.Artist!.Name.Should().Be("New Artist");
        track.Album.Id.Should().NotBe(before.Album!.Id);
        (track.Year, track.TrackNumber, track.DiscNumber, track.Genre).Should().Be((2020, 7, 2, "Jazz"));
        (await _admin.GetAsync($"/api/v1/albums/{before.Album.Id}")).StatusCode
            .Should().Be(HttpStatusCode.OK, "the other tracks of the folder keep the old album");

        using (var file = TagLib.File.Create(_media.Combine("Band/Album/03 - Changed.mp3")))
        {
            (file.Tag.Title, file.Tag.FirstPerformer, file.Tag.FirstAlbumArtist, file.Tag.Album)
                .Should().Be(("Renamed", "New Artist", "New Artist", "New Album"));
            (file.Tag.Year, file.Tag.Track, file.Tag.TrackCount, file.Tag.Disc, file.Tag.FirstGenre)
                .Should().Be((2020u, 7u, 3u, 2u, "Jazz"));
        }

        TagBackup backup = (await BackupsOfAsync("Band/Album/03 - Changed.mp3")).Should().ContainSingle().Subject;
        backup.Should().BeEquivalentTo(new
        {
            Title = "Changed",
            Performers = new[] { "Band" },
            Album = "Album",
            Year = 2001,
            TrackNumber = 3,
            TrackCount = 3,
            Genres = new[] { "Rock" },
        });
    }

    [Test]
    public async Task UpdateTrackMetadata_AudioWithVideo_WritesOnlyTheAudioFile()
    {
        byte[] video = Hash("Band/Album/02 - Duo.mkv");
        byte[] audio = Hash("Band/Album/02 - Duo.flac");

        HttpResponseMessage response = await PatchAsync("Duo", """{ "genre": "Duo Genre" }""");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        Hash("Band/Album/02 - Duo.mkv").Should().Equal(video);
        Hash("Band/Album/02 - Duo.flac").Should().NotEqual(audio);
        (await GetMetadataAsync("Duo")).AudioFilePath.Should().Be("Band/Album/02 - Duo.flac");
        (await _admin.GetJsonAsync<TrackDto>($"/api/v1/tracks/{Track("Duo").Id}")).Genre.Should().Be("Duo Genre");
    }

    [Test]
    public async Task UpdateTrackMetadata_MissingMembers_StayUnchanged()
    {
        HttpResponseMessage response = await PatchAsync("Keep", """{ "year": 1999 }""");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        TrackMetadataDto metadata = await GetMetadataAsync("Keep");
        (metadata.Title, metadata.Artist, metadata.Genre, metadata.Year).Should().Be(("Keep", "Keeper", "Pop", 1999));
    }

    [Test]
    public async Task UpdateTrackMetadata_NullTitle_FallsBackToTheFileName()
    {
        HttpResponseMessage first = await PatchAsync("Loose", """{ "title": "Tagged Loose" }""");
        first.StatusCode.Should().Be(HttpStatusCode.OK, await first.Content.ReadAsStringAsync());
        (await _admin.GetJsonAsync<TrackDto>($"/api/v1/tracks/{Track("Loose").Id}")).Title.Should().Be("Tagged Loose");

        HttpResponseMessage response = await PatchAsync("Loose", """{ "title": null }""");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        (await _admin.GetJsonAsync<TrackDto>($"/api/v1/tracks/{Track("Loose").Id}")).Title.Should().Be("Loose");
        (await BackupsOfAsync("Loose.mp3")).Select(backup => backup.Title).Should().Equal(null, "Tagged Loose");
    }

    [Test]
    public async Task UpdateTrackMetadata_EmptyGenre_RemovesTheGenre()
    {
        HttpResponseMessage response = await PatchAsync("Genre Song", """{ "genre": "  " }""");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        (await _admin.GetJsonAsync<TrackDto>($"/api/v1/tracks/{Track("Genre Song").Id}")).Genre.Should().BeNull();
        using var file = TagLib.File.Create(_media.Combine("Genre.mp3"));
        file.Tag.Genres.Should().BeEmpty();
    }

    [Test]
    public async Task UpdateTrackMetadata_EmptyObject_ReturnsTheMetadataWithoutWriting()
    {
        DateTime modified = File.GetLastWriteTimeUtc(_media.Combine("Empty.mp3"));

        HttpResponseMessage response = await PatchAsync("Empty", "{}");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        (await response.Content.ReadFromJsonAsync<TrackMetadataDto>(HttpClientExtensions.JsonOptions))!.Title
            .Should().Be("Empty");
        File.GetLastWriteTimeUtc(_media.Combine("Empty.mp3")).Should().Be(modified);
        (await BackupsOfAsync("Empty.mp3")).Should().BeEmpty();
    }

    [TestCase("""{ "year": 0 }""", "year")]
    [TestCase("""{ "trackNumber": 10000 }""", "trackNumber")]
    [TestCase("""{ "discNumber": 0 }""", "discNumber")]
    [TestCase("""{ "year": "2001" }""", "year")]
    public async Task UpdateTrackMetadata_InvalidValue_ReturnsAValidationProblem(string body, string field)
    {
        HttpResponseMessage response = await PatchAsync("Empty", body);

        // A value of the wrong type also makes the framework report the whole body as missing (field "").
        IReadOnlyList<ValidationErrorDto> errors = await response.ShouldBeValidationProblemAsync();
        errors.Select(error => error.Field).Should().Contain(field);
        errors.Select(error => error.Field).Should().BeSubsetOf([field, string.Empty]);
    }

    [Test]
    public async Task UpdateTrackMetadata_TooLongTexts_ReturnsAValidationProblem()
    {
        string body = $$"""{ "title": "{{new string('t', 513)}}", "genre": "{{new string('g', 129)}}" }""";

        HttpResponseMessage response = await PatchAsync("Empty", body);

        IReadOnlyList<ValidationErrorDto> errors = await response.ShouldBeValidationProblemAsync();
        errors.Select(error => error.Field).Should().BeEquivalentTo("title", "genre");
    }

    [Test]
    public async Task OpenApi_TrackMetadata_IsDescribedWithItsConflicts()
    {
        JsonNode document = JsonNode.Parse(await _anonymous.GetStringAsync("/openapi/v1.json"))!;

        JsonNode path = document["paths"]!["/api/v1/tracks/{id}/metadata"]!;
        path["get"]!["operationId"]!.GetValue<string>().Should().Be("GetTrackMetadata");
        path["patch"]!["operationId"]!.GetValue<string>().Should().Be("UpdateTrackMetadata");
        path["patch"]!["responses"]!["409"].Should().NotBeNull();
        JsonArray codes = document["components"]!["schemas"]!["ProblemCode"]!["enum"]!.AsArray();
        codes.Select(code => code!.GetValue<string>()).Should().Contain(["track.no_audio", "track.not_writable"]);
        document["components"]!["schemas"]!["UpdateTrackMetadata"]!["properties"]!.AsObject()
            .Select(property => property.Key)
            .Should().BeEquivalentTo(
                "title",
                "artist",
                "album",
                "albumArtist",
                "year",
                "trackNumber",
                "discNumber",
                "genre");
    }

    private TrackDto Track(string title)
    {
        return _tracks.Single(track => track.Title == title);
    }

    private Task<TrackMetadataDto> GetMetadataAsync(string title)
    {
        return _admin.GetJsonAsync<TrackMetadataDto>($"/api/v1/tracks/{Track(title).Id}/metadata");
    }

    private Task<HttpResponseMessage> PatchAsync(string title, string body)
    {
        return _admin.SendJsonAsync("PATCH", $"/api/v1/tracks/{Track(title).Id}/metadata", body);
    }

    private async Task<List<TagBackup>> BackupsOfAsync(string relativePath)
    {
        await using AsyncServiceScope scope = _factory.Services.CreateAsyncScope();
        WaveMpDbContext db = scope.ServiceProvider.GetRequiredService<WaveMpDbContext>();
        return await db.TagBackups.AsNoTracking()
            .Where(backup => backup.RelativePath == relativePath)
            .OrderBy(backup => backup.CreatedAt)
            .ThenBy(backup => backup.Id)
            .ToListAsync();
    }

    private byte[] Hash(string relativePath)
    {
        using FileStream stream = File.OpenRead(_media.Combine(relativePath));
        return SHA256.HashData(stream);
    }

    private Task AudioAsync(string relativePath, TestTags? tags)
    {
        return TestMediaGenerator.CreateAudioAsync(_media.Combine(relativePath), AudioFormat.Mp3, tags);
    }
}
