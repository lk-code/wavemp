using AwesomeAssertions;
using NSubstitute;
using WaveMP.Core.Metadata.Files;
using WaveMP.Core.Streaming.Decisions;
using WaveMP.Core.Streaming.Formats;
using WaveMP.Core.Streaming.Sources;
using WaveMP.Data.Entities;
using WaveMP.Data.Projections;
using WaveMP.Data.Repositories;
using WaveMP.Testing.Files;

namespace WaveMP.Core.Streaming.Tests.Sources;

[TestFixture]
public class StreamServiceTests
{
    private const string RelativePath = "Band/Album/01 - Song.flac";

    private TemporaryDirectory _media = null!;
    private ITrackRepository _tracks = null!;
    private IMediaFileLocator _locator = null!;
    private StreamService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _media = new TemporaryDirectory();
        _tracks = Substitute.For<ITrackRepository>();
        _locator = Substitute.For<IMediaFileLocator>();
        _locator.TryGetFullPath(Arg.Any<string>(), out Arg.Any<string>()).Returns(call =>
        {
            string relative = call.ArgAt<string>(0);
            if (relative.Contains("..", StringComparison.Ordinal))
            {
                call[1] = string.Empty;
                return false;
            }

            call[1] = _media.Combine(relative);
            return true;
        });
        _service = new StreamService(_tracks, _locator);
    }

    [TearDown]
    public void TearDown()
    {
        _media.Dispose();
    }

    [Test]
    public async Task GetAudioSourceAsync_ExistingFile_ReturnsItWithTheValuesOfTheDisk()
    {
        var trackId = Guid.NewGuid();
        MediaFile file = AudioFile(RelativePath, "flac", "flac", "flac");
        file.Size = 1;
        file.Duration = TimeSpan.FromSeconds(3);
        file.Tags = new MediaFileTags
        {
            ReplayGainTrackGain = -6.5,
            ReplayGainTrackPeak = 0.9,
            ReplayGainAlbumGain = -7.25,
            ReplayGainAlbumPeak = 0.95,
        };
        string path = WriteFile(RelativePath, 1234);
        DateTime modified = new(2026, 5, 4, 3, 2, 1, 500, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(path, modified);
        Track(trackId, file);

        StreamSourceResult result = await _service.GetAudioSourceAsync(trackId);

        result.Status.Should().Be(StreamSourceStatus.Found);
        StreamSource source = result.Source!;
        source.TrackId.Should().Be(trackId);
        source.FullPath.Should().Be(path);
        source.Format.Should().Be(AudioFormat.Flac);
        source.ContentType.Should().Be("audio/flac");
        source.Length.Should().Be(1234);
        source.LastModified.Should().Be(new DateTimeOffset(2026, 5, 4, 3, 2, 1, TimeSpan.Zero));
        source.ETag.Should().NotBeNullOrEmpty().And.NotContain("\"");
        source.Duration.Should().Be(TimeSpan.FromSeconds(3));
        source.ReplayGain.Should().Be(new ReplayGainValues(-6.5, 0.9, -7.25, 0.95));
    }

    [Test]
    public async Task GetAudioSourceAsync_ChangedFile_GetsAnotherETag()
    {
        var trackId = Guid.NewGuid();
        Track(trackId, AudioFile(RelativePath, "flac", "flac", "flac"));
        WriteFile(RelativePath, 100);
        StreamSource before = (await _service.GetAudioSourceAsync(trackId)).Source!;

        WriteFile(RelativePath, 200);
        StreamSource after = (await _service.GetAudioSourceAsync(trackId)).Source!;

        after.ETag.Should().NotBe(before.ETag);
    }

    [Test]
    public async Task GetAudioSourceAsync_UnknownFormat_DeliversItAsBinary()
    {
        var trackId = Guid.NewGuid();
        Track(trackId, AudioFile("Song.wma", "asf", "wmav2", "wma"));
        WriteFile("Song.wma", 10);

        StreamSource source = (await _service.GetAudioSourceAsync(trackId)).Source!;

        source.Format.Should().BeNull();
        source.ContentType.Should().Be("application/octet-stream");
        source.ReplayGain.Should().Be(ReplayGainValues.None);
    }

    [Test]
    public async Task GetAudioSourceAsync_UnknownTrack_ReturnsTrackNotFound()
    {
        _tracks.FindPrimaryFileAsync(Arg.Any<Guid>(), MediaFileKind.Audio, Arg.Any<CancellationToken>())
            .Returns((TrackFileRow?)null);

        StreamSourceResult result = await _service.GetAudioSourceAsync(Guid.NewGuid());

        result.Status.Should().Be(StreamSourceStatus.TrackNotFound);
        result.Source.Should().BeNull();
    }

    [Test]
    public async Task GetAudioSourceAsync_TrackWithoutAudio_ReturnsNoAudio()
    {
        var trackId = Guid.NewGuid();
        Track(trackId, null);

        StreamSourceResult result = await _service.GetAudioSourceAsync(trackId);

        result.Status.Should().Be(StreamSourceStatus.NoAudio);
    }

    [Test]
    public async Task GetAudioSourceAsync_FileGoneSinceTheScan_ReturnsFileMissing()
    {
        var trackId = Guid.NewGuid();
        Track(trackId, AudioFile(RelativePath, "flac", "flac", "flac"));

        StreamSourceResult result = await _service.GetAudioSourceAsync(trackId);

        result.Status.Should().Be(StreamSourceStatus.FileMissing);
    }

    [Test]
    public async Task GetAudioSourceAsync_PathOutsideTheMediaFolder_ReturnsFileMissing()
    {
        var trackId = Guid.NewGuid();
        Track(trackId, AudioFile("../secret.mp3", "mp3", "mp3", "mp3"));

        StreamSourceResult result = await _service.GetAudioSourceAsync(trackId);

        result.Status.Should().Be(StreamSourceStatus.FileMissing);
    }

    [Test]
    public async Task DecideAsync_SupportedFormat_PlaysDirectly()
    {
        var trackId = Guid.NewGuid();
        Track(trackId, AudioFile("Song.mp3", "mp3", "mp3", "mp3"));
        WriteFile("Song.mp3", 10);

        PlaybackResult result = await _service.DecideAsync(trackId, new HashSet<AudioFormat> { AudioFormat.Mp3 });

        result.IsFound.Should().BeTrue();
        result.Source!.ContentType.Should().Be("audio/mpeg");
        result.Decision.Should().Be(new PlaybackDecision(PlaybackMethod.DirectPlay, AudioFormat.Mp3, null));
    }

    [Test]
    public async Task DecideAsync_UnsupportedFormat_NeedsTranscoding()
    {
        var trackId = Guid.NewGuid();
        Track(trackId, AudioFile(RelativePath, "flac", "flac", "flac"));
        WriteFile(RelativePath, 10);

        PlaybackResult result = await _service.DecideAsync(trackId, new HashSet<AudioFormat> { AudioFormat.Mp3 });

        result.Decision!.Method.Should().Be(PlaybackMethod.Transcode);
        result.Decision.Reason.Should().Be(TranscodeReason.FormatUnsupported);
    }

    [Test]
    public async Task DecideAsync_MissingTrack_HasNoDecision()
    {
        _tracks.FindPrimaryFileAsync(Arg.Any<Guid>(), MediaFileKind.Audio, Arg.Any<CancellationToken>())
            .Returns((TrackFileRow?)null);

        PlaybackResult result = await _service.DecideAsync(Guid.NewGuid(), new HashSet<AudioFormat>());

        result.IsFound.Should().BeFalse();
        result.Status.Should().Be(StreamSourceStatus.TrackNotFound);
        result.Decision.Should().BeNull();
    }

    [Test]
    public async Task GetVideoSourceAsync_ExistingVideo_ReturnsItsFormat()
    {
        var trackId = Guid.NewGuid();
        MediaFile file = VideoFile("Clip.mp4", "mov,mp4,m4a,3gp,3g2,mj2", "h264", "aac", "mp4");
        file.Width = 1920;
        file.Height = 1080;
        file.SampleRate = 48000;
        file.Duration = TimeSpan.FromMinutes(3);
        string path = WriteFile("Clip.mp4", 500);
        Video(trackId, file);

        VideoSourceResult result = await _service.GetVideoSourceAsync(trackId);

        result.Status.Should().Be(StreamSourceStatus.Found);
        VideoSource source = result.Source!;
        source.Should().BeEquivalentTo(new
        {
            TrackId = trackId,
            FileId = file.Id,
            FullPath = path,
            Container = VideoContainer.Mp4,
            VideoCodec = VideoCodec.H264,
            AudioCodec = AudioCodec.Aac,
            HasAudio = true,
            PixelFormat = "yuv420p",
            Width = 1920,
            Height = 1080,
            SampleRate = 48000,
            ContentType = "video/mp4",
            Length = 500L,
            MetadataComplete = true,
        });
        source.ETag.Should().StartWith(file.Id.ToString("N"));
    }

    [Test]
    public async Task GetVideoSourceAsync_TrackWithOffset_CarriesIt()
    {
        var trackId = Guid.NewGuid();
        MediaFile file = VideoFile("Clip.mp4", "mov,mp4,m4a,3gp,3g2,mj2", "h264", "aac", "mp4");
        WriteFile("Clip.mp4", 50);
        Video(trackId, file, -2500);

        VideoSource source = (await _service.GetVideoSourceAsync(trackId)).Source!;

        source.OffsetMilliseconds.Should().Be(-2500);
    }

    [Test]
    public async Task GetVideoSourceAsync_VideoWithoutSoundReadByAnOlderScan_IsIncomplete()
    {
        var trackId = Guid.NewGuid();
        MediaFile file = VideoFile("Clip.avi", "avi", "mpeg4", null, "avi");
        file.MetadataVersion = 1;
        WriteFile("Clip.avi", 10);
        Video(trackId, file);

        VideoSource source = (await _service.GetVideoSourceAsync(trackId)).Source!;

        source.Container.Should().BeNull();
        source.VideoCodec.Should().BeNull();
        source.HasAudio.Should().BeFalse();
        source.ContentType.Should().Be("application/octet-stream");
        source.MetadataComplete.Should().BeFalse();
    }

    [Test]
    public async Task GetVideoSourceAsync_TrackWithoutVideo_ReturnsNoVideo()
    {
        var trackId = Guid.NewGuid();
        Video(trackId, null);

        (await _service.GetVideoSourceAsync(trackId)).Status.Should().Be(StreamSourceStatus.NoVideo);
    }

    [Test]
    public async Task GetVideoSourceAsync_FileGone_ReturnsFileMissing()
    {
        var trackId = Guid.NewGuid();
        Video(trackId, VideoFile("Clip.mp4", "mov,mp4,m4a,3gp,3g2,mj2", "h264", "aac", "mp4"));

        (await _service.GetVideoSourceAsync(trackId)).Status.Should().Be(StreamSourceStatus.FileMissing);
    }

    [Test]
    public async Task GetVideoSourceAsync_UnknownTrack_ReturnsTrackNotFound()
    {
        _tracks.FindPrimaryFileAsync(Arg.Any<Guid>(), MediaFileKind.Video, Arg.Any<CancellationToken>())
            .Returns((TrackFileRow?)null);

        (await _service.GetVideoSourceAsync(Guid.NewGuid())).Status.Should().Be(StreamSourceStatus.TrackNotFound);
    }

    [Test]
    public async Task DecideVideoAsync_SupportedVideo_PlaysDirectly()
    {
        var trackId = Guid.NewGuid();
        Video(trackId, VideoFile("Clip.webm", "matroska,webm", "vp9", "opus", "webm"));
        WriteFile("Clip.webm", 10);
        VideoCapabilities capabilities = new(
            new HashSet<VideoContainer> { VideoContainer.Webm },
            new HashSet<VideoCodec> { VideoCodec.Vp9 },
            new HashSet<AudioCodec> { AudioCodec.Opus });

        VideoPlaybackResult result = await _service.DecideVideoAsync(trackId, capabilities);

        result.IsFound.Should().BeTrue();
        result.Decision.Should().Be(VideoPlaybackDecision.DirectPlay);
        result.Source!.ContentType.Should().Be("video/webm");
    }

    [Test]
    public async Task DecideVideoAsync_WithoutCapabilities_Transcodes()
    {
        var trackId = Guid.NewGuid();
        Video(trackId, VideoFile("Clip.webm", "matroska,webm", "vp9", "opus", "webm"));
        WriteFile("Clip.webm", 10);

        VideoPlaybackResult result = await _service.DecideVideoAsync(trackId, VideoCapabilities.None);

        result.Decision!.Method.Should().Be(PlaybackMethod.Transcode);
    }

    [Test]
    public async Task DecideVideoAsync_NoVideo_HasNoDecision()
    {
        var trackId = Guid.NewGuid();
        Video(trackId, null);

        VideoPlaybackResult result = await _service.DecideVideoAsync(trackId, VideoCapabilities.None);

        result.IsFound.Should().BeFalse();
        result.Status.Should().Be(StreamSourceStatus.NoVideo);
    }

    [Test]
    public async Task DecideAsync_TrackWithVideoOnly_TranscodesTheSoundOfTheVideo()
    {
        var trackId = Guid.NewGuid();
        Track(trackId, null);
        MediaFile video = VideoFile("Clip.mp4", "mov,mp4,m4a,3gp,3g2,mj2", "h264", "aac", "mp4");
        Video(trackId, video);
        string path = WriteFile("Clip.mp4", 10);

        PlaybackResult result = await _service.DecideAsync(trackId, new HashSet<AudioFormat> { AudioFormat.Mp4Aac });

        result.IsFound.Should().BeTrue();
        result.Source!.Origin.Should().Be(MediaFileKind.Video);
        result.Source.FileId.Should().Be(video.Id);
        result.Source.FullPath.Should().Be(path);
        result.Source.Format.Should().BeNull();
        result.Source.ReplayGain.Should().Be(ReplayGainValues.None);
        result.Decision.Should().Be(new PlaybackDecision(
            PlaybackMethod.Transcode,
            null,
            TranscodeReason.AudioFromVideo));
    }

    [Test]
    public async Task GetSoundSourceAsync_TrackWithoutAudioAndVideo_ReturnsNoAudio()
    {
        var trackId = Guid.NewGuid();
        Track(trackId, null);
        Video(trackId, null);

        (await _service.GetSoundSourceAsync(trackId)).Status.Should().Be(StreamSourceStatus.NoAudio);
    }

    [Test]
    public async Task GetSoundSourceAsync_VideoGone_ReturnsFileMissing()
    {
        var trackId = Guid.NewGuid();
        Track(trackId, null);
        Video(trackId, VideoFile("Clip.mp4", "mov,mp4,m4a,3gp,3g2,mj2", "h264", "aac", "mp4"));

        (await _service.GetSoundSourceAsync(trackId)).Status.Should().Be(StreamSourceStatus.FileMissing);
    }

    [Test]
    public async Task GetAudioSourceAsync_TrackWithVideoOnly_StillReturnsNoAudio()
    {
        var trackId = Guid.NewGuid();
        Track(trackId, null);
        Video(trackId, VideoFile("Clip.mp4", "mov,mp4,m4a,3gp,3g2,mj2", "h264", "aac", "mp4"));
        WriteFile("Clip.mp4", 10);

        (await _service.GetAudioSourceAsync(trackId)).Status.Should().Be(StreamSourceStatus.NoAudio);
    }

    private void Track(Guid trackId, MediaFile? file)
    {
        _tracks.FindPrimaryFileAsync(trackId, MediaFileKind.Audio, Arg.Any<CancellationToken>())
            .Returns(new TrackFileRow(trackId, file));
    }

    private void Video(Guid trackId, MediaFile? file, int offsetMilliseconds = 0)
    {
        _tracks.FindPrimaryFileAsync(trackId, MediaFileKind.Video, Arg.Any<CancellationToken>())
            .Returns(new TrackFileRow(trackId, file, offsetMilliseconds));
    }

    private static MediaFile VideoFile(
        string relativePath,
        string format,
        string codec,
        string? audioCodec,
        string extension)
    {
        MediaFile file = AudioFile(relativePath, format, codec, extension);
        file.Kind = MediaFileKind.Video;
        file.AudioCodec = audioCodec;
        file.PixelFormat = "yuv420p";
        file.IsWritable = false;
        file.MetadataVersion = 2;
        return file;
    }

    private string WriteFile(string relativePath, int length)
    {
        string path = _media.Combine(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, new byte[length]);
        return path;
    }

    private static MediaFile AudioFile(string relativePath, string format, string codec, string extension)
    {
        string fileName = relativePath[(relativePath.LastIndexOf('/') + 1)..];
        string baseName = Path.GetFileNameWithoutExtension(fileName);
        return new MediaFile
        {
            Id = Guid.NewGuid(),
            RelativePath = relativePath,
            DirectoryPath = string.Empty,
            FileName = fileName,
            BaseName = baseName,
            NormalizedBaseName = baseName.ToUpperInvariant(),
            Extension = extension,
            Kind = MediaFileKind.Audio,
            Size = 10,
            ModifiedAt = DateTimeOffset.UnixEpoch,
            IsPrimary = true,
            Format = format,
            Codec = codec,
            IsLossless = false,
            HasEmbeddedPicture = false,
            IsWritable = true,
            MetadataVersion = 1,
            FirstSeenAt = DateTimeOffset.UnixEpoch,
            ScannedAt = DateTimeOffset.UnixEpoch,
        };
    }
}
