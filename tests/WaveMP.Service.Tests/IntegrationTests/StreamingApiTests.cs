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
/// Plays a library of synthetic media through the API: the decision between direct play and transcoding, the signed
/// stream URLs and the range requests of the stream. The file watcher is off, so only the tests change the library.
/// </summary>
[TestFixture]
[Category("Integration")]
public class StreamingApiTests
{
    private const string FlacFormats = "audioFormats=Flac&audioFormats=Mp3";

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
        await TestMediaGenerator.CreateAudioAsync(
            _media.Combine("Band/Album/01 - Gain.flac"),
            AudioFormat.Flac,
            new TestTags
            {
                Title = "Gain",
                Artist = "Band",
                Album = "Album",
                ReplayGainTrackGain = "-6.50 dB",
                ReplayGainTrackPeak = "0.988525",
                ReplayGainAlbumGain = "-7.25 dB",
                ReplayGainAlbumPeak = "0.999969",
            });
        await TestMediaGenerator.CreateAudioAsync(
            _media.Combine("Band/Album/02 - Plain.mp3"),
            AudioFormat.Mp3,
            new TestTags { Title = "Plain", Artist = "Band", Album = "Album" });
        await TestMediaGenerator.CreateAudioAsync(
            _media.Combine("Opus.opus"),
            AudioFormat.Opus,
            new TestTags { Title = "Opus" });
        await TestMediaGenerator.CreateAudioAsync(_media.Combine("Matroska.opus.tmp"), AudioFormat.Opus);
        await FfmpegTool.RunAsync(
            "-i",
            _media.Combine("Matroska.opus.tmp"),
            "-c",
            "copy",
            "-metadata",
            "title=Matroska",
            "-f",
            "matroska",
            _media.Combine("Matroska.mka"));
        File.Delete(_media.Combine("Matroska.opus.tmp"));
        await TestMediaGenerator.CreateVideoAsync(
            _media.Combine("Video Only.mp4"),
            VideoFormat.Mp4,
            320,
            240,
            new TestTags { Title = "Video Only" });
        await TestMediaGenerator.CreateAudioAsync(
            _media.Combine("Vanished.mp3"),
            AudioFormat.Mp3,
            new TestTags { Title = "Vanished" });

        _factory = new WaveMpWebApplicationFactory(
            WaveMpWebApplicationFactory.TestDatabaseName + "_streaming",
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
        File.Delete(_media.Combine("Vanished.mp3"));
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

    [Test]
    public async Task GetTrackPlayback_WithoutToken_ReturnsUnauthorized()
    {
        HttpResponseMessage response = await _anonymous.GetAsync(PlaybackPath("Gain", FlacFormats));

        await response.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, "auth.unauthorized");
    }

    [Test]
    public async Task GetTrackPlayback_SupportedFormat_ReturnsDirectPlayWithSignedStreamUrl()
    {
        DateTimeOffset before = DateTimeOffset.UtcNow;

        HttpResponseMessage response = await _user.GetAsync(PlaybackPath("Gain", FlacFormats));

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
        TrackPlaybackDto playback =
            (await response.Content.ReadFromJsonAsync<TrackPlaybackDto>(HttpClientExtensions.JsonOptions))!;
        playback.TrackId.Should().Be(Track("Gain").Id);
        playback.Method.Should().Be(PlaybackMethodDto.DirectPlay);
        playback.Format.Should().Be(AudioFormatDto.Flac);
        playback.TranscodeReason.Should().BeNull();
        playback.ContentType.Should().Be("audio/flac");
        playback.StreamUrl.Should().StartWith($"/api/v1/tracks/{Track("Gain").Id}/stream?uid=");
        playback.StreamUrl.Should().Contain("&sid=").And.Contain("&exp=").And.Contain("&sig=");
        playback.ExpiresAt.Should().BeAfter(before.AddMinutes(59)).And.BeBefore(before.AddHours(2));
        playback.DurationMilliseconds.Should().BeInRange(900, 1100);
        playback.ReplayGain.TrackGain.Should().BeApproximately(-6.5, 0.001);
        playback.ReplayGain.TrackPeak.Should().BeApproximately(0.988525, 0.000001);
        playback.ReplayGain.AlbumGain.Should().BeApproximately(-7.25, 0.001);
        playback.ReplayGain.AlbumPeak.Should().BeApproximately(0.999969, 0.000001);
    }

    [Test]
    public async Task GetTrackPlayback_WithoutReplayGainTags_ReturnsEmptyReplayGain()
    {
        TrackPlaybackDto playback = await PlaybackAsync("Plain", "audioFormats=Mp3");

        playback.Method.Should().Be(PlaybackMethodDto.DirectPlay);
        playback.Format.Should().Be(AudioFormatDto.Mp3);
        playback.ContentType.Should().Be("audio/mpeg");
        playback.ReplayGain.Should().BeEquivalentTo(new ReplayGainDto());
    }

    [Test]
    public async Task GetTrackPlayback_OggOpus_ReturnsDirectPlayAsOgg()
    {
        TrackPlaybackDto playback = await PlaybackAsync("Opus", "audioFormats=OggOpus");

        playback.Method.Should().Be(PlaybackMethodDto.DirectPlay);
        playback.Format.Should().Be(AudioFormatDto.OggOpus);
        playback.ContentType.Should().Be("audio/ogg");
    }

    [Test]
    public async Task GetTrackPlayback_UnsupportedFormat_ReturnsTranscodeWithoutStream()
    {
        HttpResponseMessage response = await _user.GetAsync(PlaybackPath("Gain", "audioFormats=Mp3"));

        string content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, content);
        content.Should().Contain("\"transcodeReason\":\"format_unsupported\"");
        TrackPlaybackDto playback =
            (await response.Content.ReadFromJsonAsync<TrackPlaybackDto>(HttpClientExtensions.JsonOptions))!;
        playback.Method.Should().Be(PlaybackMethodDto.Transcode);
        playback.Format.Should().Be(AudioFormatDto.Flac);
        playback.TranscodeReason.Should().Be(TranscodeReasonDto.FormatUnsupported);
        playback.StreamUrl.Should().BeNull();
        playback.ExpiresAt.Should().BeNull();
        playback.ContentType.Should().BeNull();
        playback.ReplayGain.TrackGain.Should().NotBeNull();
    }

    [Test]
    public async Task GetTrackPlayback_UnknownFormat_ReturnsTranscodeWithoutFormat()
    {
        TrackPlaybackDto playback = await PlaybackAsync("Matroska", "audioFormats=OggOpus&audioFormats=WebmOpus");

        playback.Method.Should().Be(PlaybackMethodDto.Transcode);
        playback.Format.Should().BeNull();
        playback.TranscodeReason.Should().Be(TranscodeReasonDto.FormatUnknown);
        playback.StreamUrl.Should().BeNull();
    }

    [Test]
    public async Task GetTrackPlayback_WithoutFormats_ReturnsRequired()
    {
        HttpResponseMessage response = await _user.GetAsync(PlaybackPath("Gain", string.Empty));

        IReadOnlyList<ValidationErrorDto> errors = await response.ShouldBeValidationProblemAsync();
        errors.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new { Field = "audioFormats", Code = ValidationErrorCodeDto.Required });
    }

    [Test]
    public async Task GetTrackPlayback_TooManyFormats_ReturnsMaxItems()
    {
        string query = string.Join('&', Enumerable.Repeat("audioFormats=Mp3", 21));

        HttpResponseMessage response = await _user.GetAsync(PlaybackPath("Gain", query));

        IReadOnlyList<ValidationErrorDto> errors = await response.ShouldBeValidationProblemAsync();
        errors.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new { Field = "audioFormats", Code = ValidationErrorCodeDto.MaxItems, Max = 20 });
    }

    [Test]
    public async Task GetTrackPlayback_UndefinedFormatNumber_ReturnsInvalidFormat()
    {
        HttpResponseMessage response = await _user.GetAsync(
            PlaybackPath("Gain", "audioFormats=Mp3&audioFormats=99"));

        IReadOnlyList<ValidationErrorDto> errors = await response.ShouldBeValidationProblemAsync();
        errors.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new { Field = "audioFormats", Code = ValidationErrorCodeDto.InvalidFormat });
    }

    [Test]
    public async Task GetTrackPlayback_UnknownFormatName_ReturnsValidationProblem()
    {
        HttpResponseMessage response = await _user.GetAsync(PlaybackPath("Gain", "audioFormats=Wma"));

        IReadOnlyList<ValidationErrorDto> errors = await response.ShouldBeValidationProblemAsync();
        errors.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new { Field = "audioFormats", Code = ValidationErrorCodeDto.InvalidFormat });
    }

    [Test]
    public async Task GetTrackPlayback_UnknownTrack_ReturnsNotFound()
    {
        HttpResponseMessage response = await _user.GetAsync(
            $"/api/v1/tracks/{Guid.NewGuid()}/playback?{FlacFormats}");

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "track.not_found");
    }

    [Test]
    public async Task GetTrackPlayback_VideoOnly_ReturnsTranscodeFromVideo()
    {
        TrackPlaybackDto playback = await PlaybackAsync("Video Only", FlacFormats);

        playback.Method.Should().Be(PlaybackMethodDto.Transcode);
        playback.TranscodeReason.Should().Be(TranscodeReasonDto.AudioFromVideo);
        playback.Format.Should().BeNull();
        playback.ReplayGain.Should().BeEquivalentTo(new ReplayGainDto());
    }

    [Test]
    public async Task GetTrackPlayback_FileGone_ReturnsFileMissing()
    {
        HttpResponseMessage response = await _user.GetAsync(PlaybackPath("Vanished", "audioFormats=Mp3"));

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "stream.file_missing");
    }

    [Test]
    public async Task StreamTrack_SignedUrl_ReturnsWholeFileWithoutToken()
    {
        TrackPlaybackDto playback = await PlaybackAsync("Gain", FlacFormats);
        byte[] expected = await File.ReadAllBytesAsync(_media.Combine("Band/Album/01 - Gain.flac"));

        HttpResponseMessage response = await _anonymous.GetAsync(playback.StreamUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("audio/flac");
        response.Content.Headers.ContentLength.Should().Be(expected.Length);
        response.Content.Headers.LastModified.Should().NotBeNull();
        response.Headers.AcceptRanges.Should().Contain("bytes");
        response.Headers.ETag!.IsWeak.Should().BeFalse();
        response.Headers.CacheControl!.Private.Should().BeTrue();
        response.Headers.CacheControl.NoCache.Should().BeTrue();
        response.Headers.GetValues("X-Content-Type-Options").Should().Equal("nosniff");
        (await response.Content.ReadAsByteArrayAsync()).Should().Equal(expected);
    }

    [Test]
    public async Task StreamTrack_Range_ReturnsPartialContent()
    {
        TrackPlaybackDto playback = await PlaybackAsync("Gain", FlacFormats);
        byte[] file = await File.ReadAllBytesAsync(_media.Combine("Band/Album/01 - Gain.flac"));
        using HttpRequestMessage request = new(HttpMethod.Get, playback.StreamUrl);
        request.Headers.Range = new RangeHeaderValue(100, 199);

        HttpResponseMessage response = await _anonymous.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.PartialContent);
        response.Content.Headers.ContentRange!.From.Should().Be(100);
        response.Content.Headers.ContentRange.To.Should().Be(199);
        response.Content.Headers.ContentRange.Length.Should().Be(file.Length);
        (await response.Content.ReadAsByteArrayAsync()).Should().Equal(file[100..200]);
    }

    [Test]
    public async Task StreamTrack_RangeOutsideFile_ReturnsRangeNotSatisfiable()
    {
        TrackPlaybackDto playback = await PlaybackAsync("Gain", FlacFormats);
        using HttpRequestMessage request = new(HttpMethod.Get, playback.StreamUrl);
        request.Headers.Range = new RangeHeaderValue(100_000_000, null);

        byte[] file = await File.ReadAllBytesAsync(_media.Combine("Band/Album/01 - Gain.flac"));

        HttpResponseMessage response = await _anonymous.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.RequestedRangeNotSatisfiable);
        response.Content.Headers.ContentRange!.HasRange.Should().BeFalse();
        response.Content.Headers.ContentRange.Length.Should().Be(file.Length);
        (await response.Content.ReadAsByteArrayAsync()).Should().BeEmpty();
    }

    [Test]
    public async Task StreamTrack_MatchingETag_ReturnsNotModified()
    {
        TrackPlaybackDto playback = await PlaybackAsync("Gain", FlacFormats);
        HttpResponseMessage first = await _anonymous.GetAsync(playback.StreamUrl);
        using HttpRequestMessage request = new(HttpMethod.Get, playback.StreamUrl);
        request.Headers.IfNoneMatch.Add(first.Headers.ETag!);

        HttpResponseMessage response = await _anonymous.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotModified);
    }

    [Test]
    public async Task StreamTrack_SessionToken_ReturnsFile()
    {
        HttpResponseMessage response = await _user.GetAsync($"/api/v1/tracks/{Track("Plain").Id}/stream");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("audio/mpeg");
    }

    [Test]
    public async Task StreamTrack_WithoutTokenOrSignature_ReturnsUnauthorized()
    {
        HttpResponseMessage response = await _anonymous.GetAsync($"/api/v1/tracks/{Track("Plain").Id}/stream");

        await response.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, "auth.unauthorized");
    }

    [Test]
    public async Task StreamTrack_TamperedSignature_ReturnsUnauthorized()
    {
        TrackPlaybackDto playback = await PlaybackAsync("Gain", FlacFormats);
        string url = playback.StreamUrl!;
        char last = url[^1];
        string tampered = url[..^1] + (last == 'A' ? 'B' : 'A');

        HttpResponseMessage response = await _anonymous.GetAsync(tampered);

        await response.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, "auth.unauthorized");
    }

    [Test]
    public async Task StreamTrack_SignatureOfAnotherTrack_ReturnsUnauthorized()
    {
        TrackPlaybackDto playback = await PlaybackAsync("Gain", FlacFormats);
        string url = playback.StreamUrl!.Replace(
            Track("Gain").Id.ToString(),
            Track("Plain").Id.ToString(),
            StringComparison.Ordinal);

        HttpResponseMessage response = await _anonymous.GetAsync(url);

        await response.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, "auth.unauthorized");
    }

    [Test]
    public async Task StreamTrack_ChangedExpiry_ReturnsUnauthorized()
    {
        TrackPlaybackDto playback = await PlaybackAsync("Gain", FlacFormats);
        long expires = playback.ExpiresAt!.Value.ToUnixTimeSeconds();
        string url = playback.StreamUrl!.Replace($"exp={expires}", $"exp={expires + 60}", StringComparison.Ordinal);

        HttpResponseMessage response = await _anonymous.GetAsync(url);

        await response.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, "auth.unauthorized");
    }

    [Test]
    public async Task StreamTrack_SessionEnded_ReturnsUnauthorized()
    {
        UserDto user = await _admin.CreateUserAsync(new { name = $"Leaver {HttpClientExtensions.Unique()}" });
        CreatedSessionDto session = await _anonymous.CreateSessionAsync(user.Id);
        using HttpClient client = _factory.CreateAuthorizedClient(session.Token);
        TrackPlaybackDto playback = await PlaybackAsync("Gain", FlacFormats, client);
        (await _anonymous.GetAsync(playback.StreamUrl)).StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage ended = await client.DeleteAsync("/api/v1/sessions/current");
        ended.StatusCode.Should().Be(HttpStatusCode.NoContent);
        HttpResponseMessage response = await _anonymous.GetAsync(playback.StreamUrl);

        await response.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, "auth.unauthorized");
    }

    [Test]
    public async Task SignedQuery_OnOtherEndpoint_IsNotAccepted()
    {
        TrackPlaybackDto playback = await PlaybackAsync("Gain", FlacFormats);
        string url = playback.StreamUrl!;
        string query = url[url.IndexOf('?', StringComparison.Ordinal)..];

        HttpResponseMessage track = await _anonymous.GetAsync($"/api/v1/tracks/{Track("Gain").Id}{query}");
        HttpResponseMessage playbackAgain = await _anonymous.GetAsync(
            $"/api/v1/tracks/{Track("Gain").Id}/playback{query}&{FlacFormats}");

        await track.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, "auth.unauthorized");
        await playbackAgain.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, "auth.unauthorized");
    }

    [Test]
    public async Task StreamTrack_UnsupportedFormats_ReturnsNotAcceptable()
    {
        HttpResponseMessage response = await _user.GetAsync(
            $"/api/v1/tracks/{Track("Gain").Id}/stream?audioFormats=Mp3");

        await response.ShouldBeProblemAsync(HttpStatusCode.NotAcceptable, "stream.transcoding_required");
    }

    [Test]
    public async Task StreamTrack_SupportedFormats_ReturnsFile()
    {
        HttpResponseMessage response = await _user.GetAsync(
            $"/api/v1/tracks/{Track("Gain").Id}/stream?audioFormats=Flac");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [TestCase("Video Only", "track.no_audio")]
    [TestCase("Vanished", "stream.file_missing")]
    public async Task StreamTrack_NoFile_ReturnsNotFound(string title, string code)
    {
        HttpResponseMessage response = await _user.GetAsync($"/api/v1/tracks/{Track(title).Id}/stream");

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, code);
    }

    [Test]
    public async Task StreamTrack_UnknownTrack_ReturnsNotFound()
    {
        HttpResponseMessage response = await _user.GetAsync($"/api/v1/tracks/{Guid.NewGuid()}/stream");

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "track.not_found");
    }

    private TrackDto Track(string title)
    {
        return _tracks.Single(track => track.Title == title);
    }

    private string PlaybackPath(string title, string query)
    {
        return $"/api/v1/tracks/{Track(title).Id}/playback?{query}";
    }

    private async Task<TrackPlaybackDto> PlaybackAsync(string title, string query, HttpClient? client = null)
    {
        return await (client ?? _user).GetJsonAsync<TrackPlaybackDto>(PlaybackPath(title, query));
    }
}
