using AwesomeAssertions;
using NSubstitute;
using WaveMP.Core.Metadata.Files;
using WaveMP.Core.Metadata.Probes;
using WaveMP.Core.Metadata.Readers;
using WaveMP.Core.Metadata.Tags;
using WaveMP.Data.Entities;

namespace WaveMP.Core.Metadata.Tests.Readers;

[TestFixture]
public class MediaMetadataReaderTests
{
    private const string Path = "/media/Song.mp3";

    private ITagReader _tags = null!;
    private IMediaProber _prober = null!;
    private IFileWriteAccess _writeAccess = null!;
    private MediaMetadataReader _reader = null!;

    [SetUp]
    public void SetUp()
    {
        _tags = Substitute.For<ITagReader>();
        _prober = Substitute.For<IMediaProber>();
        _writeAccess = Substitute.For<IFileWriteAccess>();
        _reader = new MediaMetadataReader(_tags, _prober, _writeAccess);
    }

    [TestCase("2001", 2001)]
    [TestCase("2001-05-12", 2001)]
    [TestCase("2001/05", 2001)]
    [TestCase("20010512", null)]
    [TestCase("May 2001", null)]
    [TestCase("99", null)]
    [TestCase(null, null)]
    public void ParseYear_Date_ReadsTheFirstFourDigits(string? date, int? expected)
    {
        MediaMetadataReader.ParseYear(date).Should().Be(expected);
    }

    [Test]
    public void FromProbeTags_WithTags_ReadsNumbersAndCounts()
    {
        MediaTags tags = MediaMetadataReader.FromProbeTags(new Dictionary<string, string>
        {
            ["title"] = " Song ",
            ["artist"] = "Band",
            ["album_artist"] = "Various",
            ["album"] = "Album",
            ["track"] = "3/12",
            ["disc"] = "2",
            ["totaldiscs"] = "3",
            ["date"] = "1999-01-01",
            ["genre"] = "Rock",
        });

        tags.Should().BeEquivalentTo(new MediaTags
        {
            Title = "Song",
            Artist = "Band",
            Artists = "Band",
            AlbumArtist = "Various",
            Album = "Album",
            TrackNumber = 3,
            TrackCount = 12,
            DiscNumber = 2,
            DiscCount = 3,
            Year = 1999,
            Genre = "Rock",
            Genres = "Rock",
        });
    }

    [Test]
    public async Task ReadAsync_WritableAudioFile_ChecksWritabilityAndUsesTheAudioStream()
    {
        _tags.Read(Path).Returns(new TagReadResult(
            TagReadStatus.Read,
            new MediaTags { Title = "Song" },
            new EmbeddedPicture("image/png", true)));
        _writeAccess.IsWritable(Path).Returns(true);
        _prober.ProbeAsync(Path, Arg.Any<CancellationToken>()).Returns(new MediaProbeResult(
            "flac",
            TimeSpan.FromSeconds(3),
            900000,
            new Dictionary<string, string> { ["title"] = "Other" },
            new AudioStreamInfo("flac", null, 96000, 2, 24),
            null,
            new PictureStreamInfo("png", 300, 300)));

        MediaFileMetadata metadata = await _reader.ReadAsync(Path, MediaFileKind.Audio);

        metadata.Tags.Title.Should().Be("Song");
        metadata.IsWritable.Should().BeTrue();
        metadata.Issue.Should().BeNull();
        metadata.Codec.Should().Be("flac");
        metadata.Bitrate.Should().Be(900000);
        metadata.SampleRate.Should().Be(96000);
        metadata.BitDepth.Should().Be(24);
        metadata.IsLossless.Should().BeTrue();
        metadata.Duration.Should().Be(TimeSpan.FromSeconds(3));
        metadata.HasEmbeddedPicture.Should().BeTrue();
        metadata.EmbeddedPictureWidth.Should().Be(300);
        metadata.EmbeddedPictureMimeType.Should().Be("image/png");
    }

    [Test]
    public async Task ReadAsync_VideoFile_IsNeverWritable()
    {
        _tags.Read(Path).Returns(new TagReadResult(TagReadStatus.Read, MediaTags.Empty, null));
        _writeAccess.IsWritable(Path).Returns(true);
        _prober.ProbeAsync(Path, Arg.Any<CancellationToken>()).Returns(new MediaProbeResult(
            "matroska,webm",
            TimeSpan.FromSeconds(3),
            5000000,
            new Dictionary<string, string>(),
            new AudioStreamInfo("aac", 128000, 48000, 2, null),
            new VideoStreamInfo("h264", 1280, 720, null),
            null));

        MediaFileMetadata metadata = await _reader.ReadAsync(Path, MediaFileKind.Video);

        metadata.IsWritable.Should().BeFalse();
        _writeAccess.DidNotReceive().IsWritable(Arg.Any<string>());
        metadata.Codec.Should().Be("h264");
        metadata.Width.Should().Be(1280);
        metadata.Bitrate.Should().Be(5000000);
        metadata.IsLossless.Should().BeFalse();
    }

    [Test]
    public async Task ReadAsync_FormatWithoutTagSupport_TakesTheTagsOfFfprobe()
    {
        _tags.Read(Path).Returns(TagReadResult.Failed(TagReadStatus.Unsupported));
        _prober.ProbeAsync(Path, Arg.Any<CancellationToken>()).Returns(new MediaProbeResult(
            "dsf",
            null,
            null,
            new Dictionary<string, string> { ["title"] = "From probe" },
            new AudioStreamInfo("dsd_lsbf_planar", null, 2822400, 2, 1),
            null,
            null));

        MediaFileMetadata metadata = await _reader.ReadAsync(Path, MediaFileKind.Audio);

        metadata.Tags.Title.Should().Be("From probe");
        metadata.Issue.Should().BeNull();
        metadata.IsWritable.Should().BeFalse();
    }

    [TestCase(TagReadStatus.Read, true, null)]
    [TestCase(TagReadStatus.Unreadable, true, MediaFileIssue.TagsUnreadable)]
    [TestCase(TagReadStatus.Read, false, MediaFileIssue.ProbeFailed)]
    [TestCase(TagReadStatus.Unreadable, false, MediaFileIssue.Unreadable)]
    [TestCase(TagReadStatus.Unsupported, false, MediaFileIssue.ProbeFailed)]
    public async Task ReadAsync_WithFailures_ReportsTheIssue(
        TagReadStatus tagStatus,
        bool probed,
        MediaFileIssue? expected)
    {
        _tags.Read(Path).Returns(new TagReadResult(tagStatus, MediaTags.Empty, null));
        _prober.ProbeAsync(Path, Arg.Any<CancellationToken>()).Returns(probed
            ? new MediaProbeResult("mp3", null, null, new Dictionary<string, string>(), null, null, null)
            : null);

        MediaFileMetadata metadata = await _reader.ReadAsync(Path, MediaFileKind.Audio);

        metadata.Issue.Should().Be(expected);
    }

    [Test]
    public async Task ReadAsync_Image_ReadsOnlyTheSizeWithFfprobe()
    {
        _prober.ProbeAsync(Path, Arg.Any<CancellationToken>()).Returns(new MediaProbeResult(
            "image2",
            TimeSpan.FromSeconds(0.04),
            null,
            new Dictionary<string, string>(),
            null,
            new VideoStreamInfo("mjpeg", 640, 480, null),
            null));

        MediaFileMetadata metadata = await _reader.ReadAsync(Path, MediaFileKind.Image);

        _tags.DidNotReceive().Read(Arg.Any<string>());
        metadata.Width.Should().Be(640);
        metadata.Height.Should().Be(480);
        metadata.Codec.Should().Be("mjpeg");
        metadata.Duration.Should().BeNull();
        metadata.Issue.Should().BeNull();
    }

    [Test]
    public async Task ReadAsync_UnreadableImage_IsUnreadable()
    {
        MediaFileMetadata metadata = await _reader.ReadAsync(Path, MediaFileKind.Image);

        metadata.Issue.Should().Be(MediaFileIssue.Unreadable);
    }
}
