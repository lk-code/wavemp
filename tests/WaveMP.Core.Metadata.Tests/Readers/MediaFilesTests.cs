using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WaveMP.Core.Metadata.Files;
using WaveMP.Core.Metadata.Options;
using WaveMP.Core.Metadata.Probes;
using WaveMP.Core.Metadata.Readers;
using WaveMP.Core.Metadata.Tags;
using WaveMP.Data.Entities;
using WaveMP.Testing.Files;
using WaveMP.Testing.Media;

namespace WaveMP.Core.Metadata.Tests.Readers;

/// <summary>
/// Reads synthetic media files created with ffmpeg through TagLib# and ffprobe.
/// </summary>
[TestFixture]
[Category("Media")]
public class MediaFilesTests
{
    private static readonly TestTags _tags = new()
    {
        Title = "Test Title",
        Artist = "Test Artist",
        Album = "Test Album",
        AlbumArtist = "Test Album Artist",
        Track = "3",
        Disc = "2",
        Date = "2001",
        Genre = "Rock",
    };

    private TemporaryDirectory _directory = null!;
    private MediaMetadataReader _reader = null!;

    [SetUp]
    public void SetUp()
    {
        _directory = new TemporaryDirectory();
        _reader = new MediaMetadataReader(new TagLibTagReader(), CreateProber(), new FileWriteAccess());
    }

    [TearDown]
    public void TearDown()
    {
        _directory.Dispose();
    }

    [TestCase(AudioFormat.Mp3, "mp3")]
    [TestCase(AudioFormat.Flac, "flac")]
    [TestCase(AudioFormat.M4a, "m4a")]
    [TestCase(AudioFormat.Opus, "opus")]
    public async Task ReadAsync_AudioWithTags_ReadsTheTags(AudioFormat format, string extension)
    {
        string path = _directory.Combine($"song.{extension}");
        await TestMediaGenerator.CreateAudioAsync(path, format, _tags);

        MediaFileMetadata metadata = await _reader.ReadAsync(path, MediaFileKind.Audio);

        metadata.Issue.Should().BeNull();
        metadata.Tags.Title.Should().Be("Test Title");
        metadata.Tags.Artist.Should().Be("Test Artist");
        metadata.Tags.Album.Should().Be("Test Album");
        metadata.Tags.AlbumArtist.Should().Be("Test Album Artist");
        metadata.Tags.TrackNumber.Should().Be(3);
        metadata.Tags.DiscNumber.Should().Be(2);
        metadata.Tags.Year.Should().Be(2001);
        metadata.Tags.Genre.Should().Be("Rock");
        metadata.Duration.Should().BeCloseTo(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(200));
        metadata.Channels.Should().Be(2);
        metadata.IsWritable.Should().BeTrue();
    }

    [Test]
    public async Task ReadAsync_VideoWithTags_ReadsTagsAndResolution()
    {
        string path = _directory.Combine("clip.mkv");
        await TestMediaGenerator.CreateVideoAsync(path, VideoFormat.Mkv, 640, 480, _tags);

        MediaFileMetadata metadata = await _reader.ReadAsync(path, MediaFileKind.Video);

        metadata.Issue.Should().BeNull();
        metadata.Tags.Title.Should().Be("Test Title");
        metadata.Tags.Artist.Should().Be("Test Artist");
        metadata.Codec.Should().Be("mpeg4");
        metadata.AudioCodec.Should().Be("aac");
        metadata.PixelFormat.Should().Be("yuv420p");
        metadata.Width.Should().Be(640);
        metadata.Height.Should().Be(480);
        metadata.IsWritable.Should().BeFalse();
    }

    [Test]
    public async Task ReadAsync_VideoWithoutSound_HasNoAudioCodec()
    {
        string path = _directory.Combine("silent.mp4");
        await TestMediaGenerator.CreateVideoAsync(
            path,
            VideoFormat.Mp4,
            320,
            240,
            codec: TestVideoCodec.H264,
            withAudio: false);

        MediaFileMetadata metadata = await _reader.ReadAsync(path, MediaFileKind.Video);

        metadata.Codec.Should().Be("h264");
        metadata.AudioCodec.Should().BeNull();
        metadata.PixelFormat.Should().Be("yuv420p");
        metadata.SampleRate.Should().BeNull();
    }

    [Test]
    public async Task ReadAsync_WebmVideo_ReadsVp9AndOpus()
    {
        string path = _directory.Combine("clip.webm");
        await TestMediaGenerator.CreateVideoAsync(path, VideoFormat.Webm, 320, 240, codec: TestVideoCodec.Vp9);

        MediaFileMetadata metadata = await _reader.ReadAsync(path, MediaFileKind.Video);

        metadata.Format.Should().Be("matroska,webm");
        metadata.Codec.Should().Be("vp9");
        metadata.AudioCodec.Should().Be("opus");
    }

    [Test]
    public async Task ReadAsync_Mp3_ReadsBitrateAndSampleRate()
    {
        string path = _directory.Combine("song.mp3");
        await TestMediaGenerator.CreateAudioAsync(path, AudioFormat.Mp3, bitrateKbps: 320, sampleRate: 48000);

        MediaFileMetadata metadata = await _reader.ReadAsync(path, MediaFileKind.Audio);

        metadata.Codec.Should().Be("mp3");
        metadata.Bitrate.Should().Be(320000);
        metadata.SampleRate.Should().Be(48000);
        metadata.IsLossless.Should().BeFalse();
    }

    [Test]
    public async Task ReadAsync_HiResFlac_ReadsBitDepthAndSampleRate()
    {
        string path = _directory.Combine("song.flac");
        await TestMediaGenerator.CreateAudioAsync(path, AudioFormat.Flac, sampleRate: 96000, bitDepth: 24);

        MediaFileMetadata metadata = await _reader.ReadAsync(path, MediaFileKind.Audio);

        metadata.Codec.Should().Be("flac");
        metadata.BitDepth.Should().Be(24);
        metadata.SampleRate.Should().Be(96000);
        metadata.IsLossless.Should().BeTrue();
        metadata.Bitrate.Should().BePositive();
    }

    [Test]
    public async Task ReadAsync_Wav_IsLosslessWithItsBitDepth()
    {
        string path = _directory.Combine("song.wav");
        await TestMediaGenerator.CreateAudioAsync(path, AudioFormat.Wav);

        MediaFileMetadata metadata = await _reader.ReadAsync(path, MediaFileKind.Audio);

        metadata.Codec.Should().Be("pcm_s16le");
        metadata.BitDepth.Should().Be(16);
        metadata.IsLossless.Should().BeTrue();
    }

    [Test]
    public async Task ReadAsync_Mp3WithEmbeddedCover_FindsThePicture()
    {
        string cover = _directory.Combine("source.jpg");
        await TestMediaGenerator.CreateImageAsync(cover, 200, 150);
        string path = _directory.Combine("song.mp3");
        await TestMediaGenerator.CreateAudioAsync(path, AudioFormat.Mp3, _tags, embeddedCoverPath: cover);

        MediaFileMetadata metadata = await _reader.ReadAsync(path, MediaFileKind.Audio);

        metadata.Issue.Should().BeNull();
        metadata.HasEmbeddedPicture.Should().BeTrue();
        metadata.EmbeddedPictureMimeType.Should().Be("image/jpeg");
        metadata.EmbeddedPictureWidth.Should().Be(200);
        metadata.EmbeddedPictureHeight.Should().Be(150);
        metadata.Width.Should().BeNull("the embedded picture is no video");
    }

    [Test]
    public async Task ReadAsync_AudioWithoutTags_HasEmptyTagsAndNoPicture()
    {
        string path = _directory.Combine("plain.mp3");
        await TestMediaGenerator.CreateAudioAsync(path, AudioFormat.Mp3);

        MediaFileMetadata metadata = await _reader.ReadAsync(path, MediaFileKind.Audio);

        metadata.Issue.Should().BeNull();
        metadata.Tags.Title.Should().BeNull();
        metadata.Tags.Artist.Should().BeNull();
        metadata.HasEmbeddedPicture.Should().BeFalse();
    }

    [TestCase("picture.jpg", 100, 80)]
    [TestCase("picture.png", 300, 300)]
    public async Task ReadAsync_Image_ReadsItsSize(string name, int width, int height)
    {
        string path = _directory.Combine(name);
        await TestMediaGenerator.CreateImageAsync(path, width, height);

        MediaFileMetadata metadata = await _reader.ReadAsync(path, MediaFileKind.Image);

        metadata.Issue.Should().BeNull();
        metadata.Width.Should().Be(width);
        metadata.Height.Should().Be(height);
    }

    [Test]
    public async Task ReadAsync_DamagedFile_ReportsAnIssueInsteadOfFailing()
    {
        string path = _directory.Combine("broken.mp3");
        TestMediaGenerator.CreateCorruptFile(path);

        MediaFileMetadata metadata = await _reader.ReadAsync(path, MediaFileKind.Audio);

        metadata.Issue.Should().BeOneOf(MediaFileIssue.ProbeFailed, MediaFileIssue.Unreadable);
    }

    [Test]
    public async Task ReadAsync_ReadOnlyAudioFile_IsNotWritable()
    {
        string path = _directory.Combine("locked.mp3");
        await TestMediaGenerator.CreateAudioAsync(path, AudioFormat.Mp3, _tags);
        File.SetAttributes(path, FileAttributes.ReadOnly);
        DateTime modified = File.GetLastWriteTimeUtc(path);

        MediaFileMetadata metadata = await _reader.ReadAsync(path, MediaFileKind.Audio);

        metadata.IsWritable.Should().BeFalse();
        metadata.Tags.Title.Should().Be("Test Title");
        File.GetLastWriteTimeUtc(path).Should().Be(modified);
    }

    [Test]
    public async Task IsWritable_WritableFile_KeepsItsChangeTime()
    {
        string path = _directory.Combine("open.mp3");
        await TestMediaGenerator.CreateAudioAsync(path, AudioFormat.Mp3);
        DateTime modified = File.GetLastWriteTimeUtc(path);

        bool writable = new FileWriteAccess().IsWritable(path);

        writable.Should().BeTrue();
        File.GetLastWriteTimeUtc(path).Should().Be(modified);
    }

    [Test]
    public async Task ProbeAsync_WithMissingFfprobe_ThrowsProbeUnavailable()
    {
        string path = _directory.Combine("song.mp3");
        await TestMediaGenerator.CreateAudioAsync(path, AudioFormat.Mp3);
        FfprobeMediaProber prober = CreateProber(_directory.Combine("missing/ffprobe"));

        Func<Task> act = () => prober.ProbeAsync(path);

        await act.Should().ThrowAsync<ProbeUnavailableException>();
        (await prober.IsAvailableAsync()).Should().BeFalse();
        (await CreateProber().IsAvailableAsync()).Should().BeTrue();
    }

    private static FfprobeMediaProber CreateProber(string? path = null)
    {
        return new FfprobeMediaProber(
            Microsoft.Extensions.Options.Options.Create(new MetadataOptions
            {
                FfprobePath = path ?? FfmpegTool.FfprobePath,
            }),
            NullLogger<FfprobeMediaProber>.Instance);
    }
}
