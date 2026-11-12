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
/// Plays synthetic videos through the API: the decision between direct play and HLS, the signed video stream with
/// range requests, the HLS playlists and segments transcoded by the real ffmpeg (segments of 2 seconds), and the
/// state of the transcoding for admins.
/// </summary>
[TestFixture]
[Category("Integration")]
public class VideoStreamingApiTests
{
    private const string BrowserCapabilities =
        "videoContainers=Mp4&videoContainers=Webm&videoCodecs=H264&videoCodecs=Vp9&audioCodecs=Aac&audioCodecs=Opus";

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
        await TestMediaGenerator.CreateVideoAsync(
            _media.Combine("Clip.mp4"),
            VideoFormat.Mp4,
            320,
            240,
            new TestTags { Title = "Clip" },
            TestVideoCodec.H264,
            durationSeconds: 7);
        await TestMediaGenerator.CreateVideoAsync(
            _media.Combine("Webm Clip.webm"),
            VideoFormat.Webm,
            320,
            240,
            new TestTags { Title = "Webm Clip" },
            TestVideoCodec.Vp9,
            durationSeconds: 3);
        await TestMediaGenerator.CreateAudioAsync(
            _media.Combine("Song.flac"),
            AudioFormat.Flac,
            new TestTags { Title = "Song" },
            durationSeconds: 5);

        _factory = new WaveMpWebApplicationFactory(
            WaveMpWebApplicationFactory.TestDatabaseName + "_video",
            freshDatabase: true,
            mediaPath: _media.Path);
        _anonymous = _factory.CreateClient();
        _admin = await _factory.CreateAdminClientAsync();
        UserDto user = await _admin.CreateUserAsync(new { name = $"Viewer {HttpClientExtensions.Unique()}" });
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

    [Test]
    public async Task GetTrackVideoPlayback_SupportedVideo_ReturnsDirectPlayWithSignedStreamUrl()
    {
        TrackVideoPlaybackDto playback = await VideoPlaybackAsync("Clip", BrowserCapabilities);

        playback.Should().BeEquivalentTo(new
        {
            TrackId = Track("Clip").Id,
            Method = PlaybackMethodDto.DirectPlay,
            Protocol = StreamProtocolDto.File,
            TranscodeReason = (TranscodeReasonDto?)null,
            Container = (VideoContainerDto?)VideoContainerDto.Mp4,
            VideoCodec = (VideoCodecDto?)VideoCodecDto.H264,
            AudioCodec = (AudioCodecDto?)AudioCodecDto.Aac,
            HasAudio = true,
            Width = (int?)320,
            Height = (int?)240,
            ContentType = "video/mp4",
        });
        playback.StreamUrl.Should().StartWith($"/api/v1/tracks/{Track("Clip").Id}/video?uid=");
        playback.DurationMilliseconds.Should().BeInRange(6500, 7500);
    }

    [Test]
    public async Task StreamTrackVideo_SignedUrlWithRange_ReturnsPartialContentWithoutToken()
    {
        TrackVideoPlaybackDto playback = await VideoPlaybackAsync("Clip", BrowserCapabilities);
        byte[] expected = await File.ReadAllBytesAsync(_media.Combine("Clip.mp4"));
        using HttpRequestMessage request = new(HttpMethod.Get, playback.StreamUrl);
        request.Headers.Range = new RangeHeaderValue(10, 109);

        HttpResponseMessage response = await _anonymous.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.PartialContent);
        response.Content.Headers.ContentType!.MediaType.Should().Be("video/mp4");
        response.Headers.CacheControl!.Private.Should().BeTrue();
        (await response.Content.ReadAsByteArrayAsync()).Should().Equal(expected[10..110]);
    }

    [Test]
    public async Task StreamTrackVideo_SignatureOfTheAudioStream_ReturnsUnauthorized()
    {
        TrackPlaybackDto audio = await _user.GetJsonAsync<TrackPlaybackDto>(
            $"/api/v1/tracks/{Track("Song").Id}/playback?audioFormats=Flac");
        string query = audio.StreamUrl![audio.StreamUrl!.IndexOf('?', StringComparison.Ordinal)..];

        HttpResponseMessage response = await _anonymous.GetAsync($"/api/v1/tracks/{Track("Song").Id}/video{query}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetTrackVideoPlayback_UnsupportedCodec_ReturnsHlsMasterPlaylist()
    {
        TrackVideoPlaybackDto playback = await VideoPlaybackAsync(
            "Webm Clip",
            "videoContainers=Mp4&videoContainers=Webm&videoCodecs=H264&audioCodecs=Aac&audioCodecs=Opus");

        playback.Method.Should().Be(PlaybackMethodDto.Transcode);
        playback.Protocol.Should().Be(StreamProtocolDto.Hls);
        playback.TranscodeReason.Should().Be(TranscodeReasonDto.VideoCodecUnsupported);
        playback.Container.Should().Be(VideoContainerDto.Webm);
        playback.VideoCodec.Should().Be(VideoCodecDto.Vp9);
        playback.AudioCodec.Should().Be(AudioCodecDto.Opus);
        playback.ContentType.Should().Be("application/vnd.apple.mpegurl");
        playback.StreamUrl.Should().StartWith(
            $"/api/v1/tracks/{Track("Webm Clip").Id}/hls/master.m3u8?media=Video&uid=");
    }

    [Test]
    public async Task Hls_WholeVideo_DeliversPlaylistsAndTranscodedSegmentsWithoutToken()
    {
        TrackVideoPlaybackDto playback = await VideoPlaybackAsync("Clip", string.Empty);
        playback.Method.Should().Be(PlaybackMethodDto.Transcode);

        HttpResponseMessage master = await _anonymous.GetAsync(playback.StreamUrl);
        string masterText = await master.Content.ReadAsStringAsync();
        master.StatusCode.Should().Be(HttpStatusCode.OK, masterText);
        master.Content.Headers.ContentType!.MediaType.Should().Be("application/vnd.apple.mpegurl");
        master.Headers.CacheControl!.NoStore.Should().BeTrue();
        masterText.Should().StartWith("#EXTM3U\n")
            .And.Contain("CODECS=\"avc1.64002a,mp4a.40.2\",RESOLUTION=320x240");

        string mediaPath = Resolve(playback.StreamUrl!, UriLines(masterText).Single());
        mediaPath.Should().MatchRegex($"^/api/v1/tracks/{Track("Clip").Id}/hls/Video/[0-9a-f]{{32}}/main\\.m3u8\\?uid=");
        HttpResponseMessage media = await _anonymous.GetAsync(mediaPath);
        string mediaText = await media.Content.ReadAsStringAsync();
        media.StatusCode.Should().Be(HttpStatusCode.OK, mediaText);
        mediaText.Should().Contain("#EXT-X-PLAYLIST-TYPE:VOD").And.EndWith("#EXT-X-ENDLIST\n");
        IReadOnlyList<string> segments = UriLines(mediaText);
        segments.Should().HaveCount(4);

        foreach (string segment in new[] { segments[0], segments[3] })
        {
            HttpResponseMessage response = await _anonymous.GetAsync(Resolve(mediaPath, segment));
            byte[] content = await response.Content.ReadAsByteArrayAsync();
            response.StatusCode.Should().Be(HttpStatusCode.OK, System.Text.Encoding.UTF8.GetString(content));
            response.Content.Headers.ContentType!.MediaType.Should().Be("video/mp2t");
            content.Length.Should().BeGreaterThan(188);
            content[0].Should().Be(0x47);
        }
    }

    [Test]
    public async Task Hls_AudioOfAFlacFile_DeliversAacSegments()
    {
        TrackPlaybackDto playback = await _user.GetJsonAsync<TrackPlaybackDto>(
            $"/api/v1/tracks/{Track("Song").Id}/playback?audioFormats=Mp3");
        playback.Protocol.Should().Be(StreamProtocolDto.Hls);

        string masterText = await _anonymous.GetStringAsync(playback.StreamUrl);
        masterText.Should().Contain("CODECS=\"mp4a.40.2\"").And.NotContain("RESOLUTION");
        string mediaPath = Resolve(playback.StreamUrl!, UriLines(masterText).Single());
        mediaPath.Should().Contain("/hls/Audio/");
        string mediaText = await _anonymous.GetStringAsync(mediaPath);
        HttpResponseMessage segment = await _anonymous.GetAsync(Resolve(mediaPath, UriLines(mediaText)[0]));

        segment.StatusCode.Should().Be(HttpStatusCode.OK);
        (await segment.Content.ReadAsByteArrayAsync())[0].Should().Be(0x47);
    }

    [Test]
    public async Task GetTrackHlsMasterPlaylist_WithSessionToken_SignsTheUris()
    {
        string masterText = await _user.GetStringAsync($"/api/v1/tracks/{Track("Clip").Id}/hls/master.m3u8");
        string uri = UriLines(masterText).Single();

        uri.Should().Contain("?uid=").And.Contain("&sig=");
        HttpResponseMessage media = await _anonymous.GetAsync(
            Resolve($"/api/v1/tracks/{Track("Clip").Id}/hls/master.m3u8", uri));
        media.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetTrackHlsMasterPlaylist_WithoutTokenOrSignature_ReturnsUnauthorized()
    {
        HttpResponseMessage response = await _anonymous.GetAsync(
            $"/api/v1/tracks/{Track("Clip").Id}/hls/master.m3u8");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetTrackHlsMediaPlaylist_OtherVariant_ReturnsVariantOutdated()
    {
        HttpResponseMessage response = await _user.GetAsync(
            $"/api/v1/tracks/{Track("Clip").Id}/hls/Video/{new string('0', 32)}/main.m3u8");

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "transcode.variant_outdated");
    }

    [Test]
    public async Task GetTrackHlsSegment_BeyondTheEnd_ReturnsSegmentNotFound()
    {
        string variantPath = await VariantPathAsync("Clip");

        HttpResponseMessage response = await _user.GetAsync($"{variantPath}/segments/4");

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "transcode.segment_not_found");
    }

    [Test]
    public async Task GetTrackHlsMasterPlaylist_VideoOfAnAudioTrack_ReturnsNoVideo()
    {
        HttpResponseMessage response = await _user.GetAsync(
            $"/api/v1/tracks/{Track("Song").Id}/hls/master.m3u8?media=Video");

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "track.no_video");
    }

    [Test]
    public async Task GetTrackHlsMasterPlaylist_UndefinedMedia_ReturnsValidationProblem()
    {
        HttpResponseMessage response = await _user.GetAsync(
            $"/api/v1/tracks/{Track("Clip").Id}/hls/master.m3u8?media=7");

        IReadOnlyList<ValidationErrorDto> errors = await response.ShouldBeValidationProblemAsync();
        errors.Select(error => error.Field).Should().Contain("media");
    }

    [Test]
    public async Task GetTrackVideoPlayback_AudioTrack_ReturnsNoVideo()
    {
        HttpResponseMessage response = await _user.GetAsync(
            $"/api/v1/tracks/{Track("Song").Id}/video/playback?{BrowserCapabilities}");

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "track.no_video");
    }

    [Test]
    public async Task GetTrackVideoPlayback_UndefinedCodecNumber_ReturnsInvalidValue()
    {
        HttpResponseMessage response = await _user.GetAsync(
            $"/api/v1/tracks/{Track("Clip").Id}/video/playback?videoCodecs=H264&videoCodecs=99");

        IReadOnlyList<ValidationErrorDto> errors = await response.ShouldBeValidationProblemAsync();
        errors.Should().ContainSingle().Which.Field.Should().StartWith("videoCodecs");
    }

    [Test]
    public async Task GetTrackVideoPlayback_UnknownTrack_ReturnsNotFound()
    {
        HttpResponseMessage response = await _user.GetAsync($"/api/v1/tracks/{Guid.NewGuid()}/video/playback");

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "track.not_found");
    }

    [Test]
    public async Task GetTranscodingStatus_Admin_ReportsFfmpegAndTheHardware()
    {
        TranscodingStatusDto status = await _admin.GetJsonAsync<TranscodingStatusDto>("/api/v1/transcoding/status");

        status.Available.Should().BeTrue();
        status.FfmpegAvailable.Should().BeTrue();
        status.CacheAvailable.Should().BeTrue();
        status.MaxParallelJobs.Should().Be(2);
        status.Hardware.State.Should().NotBe(HardwareAccelerationStateDto.Active, "the test machine has no render device");
    }

    [Test]
    public async Task ListTranscodeJobs_Admin_ReturnsAPage()
    {
        PageDto<TranscodeJobDto> page =
            await _admin.GetJsonAsync<PageDto<TranscodeJobDto>>("/api/v1/transcoding/jobs?limit=10");

        page.Limit.Should().Be(10);
        page.Items.Should().HaveCount(page.Total);
    }

    [Test]
    public async Task CancelTranscodeJob_UnknownJob_ReturnsJobNotFound()
    {
        HttpResponseMessage response = await _admin.DeleteAsync($"/api/v1/transcoding/jobs/{Guid.NewGuid()}");

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "transcode.job_not_found");
    }

    [TestCase("/api/v1/transcoding/status")]
    [TestCase("/api/v1/transcoding/jobs")]
    [TestCase("/api/v1/settings/transcoding")]
    public async Task TranscodingEndpoints_User_ReturnsForbidden(string path)
    {
        HttpResponseMessage response = await _user.GetAsync(path);

        await response.ShouldBeProblemAsync(HttpStatusCode.Forbidden, "auth.forbidden");
    }

    private TrackDto Track(string title)
    {
        return _tracks.Single(track => track.Title == title);
    }

    private async Task<TrackVideoPlaybackDto> VideoPlaybackAsync(string title, string query)
    {
        HttpResponseMessage response = await _user.GetAsync($"/api/v1/tracks/{Track(title).Id}/video/playback?{query}");
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
        return (await response.Content.ReadFromJsonAsync<TrackVideoPlaybackDto>(HttpClientExtensions.JsonOptions))!;
    }

    private async Task<string> VariantPathAsync(string title)
    {
        string path = $"/api/v1/tracks/{Track(title).Id}/hls/master.m3u8";
        string masterText = await _user.GetStringAsync(path);
        string mediaPath = Resolve(path, UriLines(masterText).Single());
        return mediaPath[..mediaPath.IndexOf("/main.m3u8", StringComparison.Ordinal)];
    }

    private static IReadOnlyList<string> UriLines(string playlist)
    {
        return [.. playlist.Split('\n').Where(line => line.Length > 0 && !line.StartsWith('#'))];
    }

    private static string Resolve(string basePath, string relative)
    {
        return new Uri(new Uri(new Uri("http://localhost"), basePath), relative).PathAndQuery;
    }
}
