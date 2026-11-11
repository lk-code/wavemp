using System.Security.Cryptography;
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

namespace WaveMP.Core.Metadata.Tests.Tags;

/// <summary>
/// Writes tags into synthetic media files created with ffmpeg and reads them back with TagLib# and ffprobe.
/// </summary>
[TestFixture]
[Category("Media")]
public class TagWriterTests
{
    private static readonly TestTags _tags = new()
    {
        Title = "Old Title",
        Artist = "Old Artist",
        Album = "Old Album",
        AlbumArtist = "Old Album Artist",
        Track = "3/12",
        Disc = "1/2",
        Date = "2001",
        Genre = "Rock",
    };

    private static readonly TagChanges _allFields = new()
    {
        Title = new TagChange<string?>("New Title"),
        Artist = new TagChange<string?>("New Artist"),
        Album = new TagChange<string?>("New Album"),
        AlbumArtist = new TagChange<string?>("New Album Artist"),
        Year = new TagChange<int?>(1999),
        TrackNumber = new TagChange<int?>(7),
        DiscNumber = new TagChange<int?>(2),
        Genre = new TagChange<string?>("Jazz"),
    };

    private TemporaryDirectory _directory = null!;
    private TagLibTagWriter _writer = null!;
    private MediaMetadataReader _reader = null!;

    [SetUp]
    public void SetUp()
    {
        _directory = new TemporaryDirectory();
        _writer = new TagLibTagWriter();
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
    [TestCase(AudioFormat.Wav, "wav")]
    public async Task Write_AllFields_CanBeReadBack(AudioFormat format, string extension)
    {
        string path = _directory.Combine($"song.{extension}");
        await TestMediaGenerator.CreateAudioAsync(path, format, _tags);
        MediaFileMetadata before = await _reader.ReadAsync(path, MediaFileKind.Audio);

        TagWriteStatus status = _writer.Write(path, MediaFileKind.Audio, _allFields);

        status.Should().Be(TagWriteStatus.Written);
        MediaFileMetadata after = await _reader.ReadAsync(path, MediaFileKind.Audio);
        after.Issue.Should().BeNull();
        after.Tags.Should().BeEquivalentTo(new
        {
            Title = "New Title",
            Artist = "New Artist",
            Album = "New Album",
            AlbumArtist = "New Album Artist",
            Year = 1999,
            TrackNumber = 7,
            DiscNumber = 2,
            Genre = "Jazz",
        });
        after.Duration.Should().Be(before.Duration);
        after.Codec.Should().Be(before.Codec);

        // Other players read the tags with other libraries, e.g. ffmpeg. Only the title is compared: in WAV files
        // ffmpeg also reads the RIFF INFO chunk, whose IART holds the artist for ffmpeg but the album artist for
        // TagLib#.
        MediaProbeResult probe = (await CreateProber().ProbeAsync(path))!;
        probe.Tags.Should().Contain("title", "New Title");
    }

    [TestCase(AudioFormat.Mp3, "mp3")]
    [TestCase(AudioFormat.Flac, "flac")]
    public async Task Write_FileWithoutTags_CreatesTheNativeTag(AudioFormat format, string extension)
    {
        string path = _directory.Combine($"plain.{extension}");
        await TestMediaGenerator.CreateAudioAsync(path, format);

        TagWriteStatus status = _writer.Write(path, MediaFileKind.Audio, _allFields);

        status.Should().Be(TagWriteStatus.Written);
        MediaFileMetadata after = await _reader.ReadAsync(path, MediaFileKind.Audio);
        after.Tags.Title.Should().Be("New Title");
        after.Tags.TrackNumber.Should().Be(7);
        using var file = TagLib.File.Create(path);
        file.TagTypesOnDisk.Should().HaveFlag(
            format == AudioFormat.Mp3 ? TagLib.TagTypes.Id3v2 : TagLib.TagTypes.Xiph);
    }

    [Test]
    public async Task Write_Mp3WithoutId3v1_AddsNoId3v1Tag()
    {
        string path = _directory.Combine("song.mp3");
        await TestMediaGenerator.CreateAudioAsync(path, AudioFormat.Mp3, _tags);
        using (var before = TagLib.File.Create(path))
        {
            before.TagTypesOnDisk.Should().NotHaveFlag(TagLib.TagTypes.Id3v1);
        }

        _writer.Write(path, MediaFileKind.Audio, _allFields).Should().Be(TagWriteStatus.Written);

        using var after = TagLib.File.Create(path);
        after.TagTypesOnDisk.Should().HaveFlag(TagLib.TagTypes.Id3v2);
        after.TagTypesOnDisk.Should().NotHaveFlag(TagLib.TagTypes.Id3v1);
    }

    [TestCase(AudioFormat.Mp3, "mp3")]
    [TestCase(AudioFormat.Flac, "flac")]
    [TestCase(AudioFormat.M4a, "m4a")]
    [TestCase(AudioFormat.Opus, "opus")]
    [TestCase(AudioFormat.Wav, "wav")]
    public async Task Write_Null_RemovesTheFields(AudioFormat format, string extension)
    {
        string path = _directory.Combine($"song.{extension}");
        await TestMediaGenerator.CreateAudioAsync(path, format, _tags);
        TagSnapshot before = ReadSnapshot(path);

        TagWriteStatus status = _writer.Write(path, MediaFileKind.Audio, new TagChanges
        {
            Title = new TagChange<string?>(null),
            Genre = new TagChange<string?>(null),
            Year = new TagChange<int?>(null),
            TrackNumber = new TagChange<int?>(null),
            DiscNumber = new TagChange<int?>(null),
        });

        status.Should().Be(TagWriteStatus.Written);
        TagSnapshot snapshot = ReadSnapshot(path);
        snapshot.Should().BeEquivalentTo(new
        {
            Title = (string?)null,
            Genres = Array.Empty<string>(),
            Year = (int?)null,
            TrackNumber = (int?)null,
            TrackCount = (int?)null,
            DiscNumber = (int?)null,
            DiscCount = (int?)null,
        });
        snapshot.Album.Should().Be(before.Album);
        snapshot.Performers.Should().Equal(before.Performers);
        snapshot.AlbumArtists.Should().Equal(before.AlbumArtists);
    }

    [TestCase(AudioFormat.Mp3, "mp3", "jpg")]
    [TestCase(AudioFormat.Flac, "flac", "png")]
    [TestCase(AudioFormat.M4a, "m4a", "jpg")]
    public async Task Write_OnlyGenre_KeepsAllOtherTagsAndTheEmbeddedCover(
        AudioFormat format,
        string extension,
        string pictureExtension)
    {
        string cover = _directory.Combine($"cover.{pictureExtension}");
        await TestMediaGenerator.CreateImageAsync(cover, 64, 64);
        string path = _directory.Combine($"song.{extension}");
        await TestMediaGenerator.CreateAudioAsync(path, format, _tags, embeddedCoverPath: cover);
        EmbeddedPictureData pictureBefore = new TagLibEmbeddedPictureReader().ReadPicture(path)!;
        MediaFileMetadata before = await _reader.ReadAsync(path, MediaFileKind.Audio);

        TagWriteStatus status = _writer.Write(path, MediaFileKind.Audio, new TagChanges
        {
            Genre = new TagChange<string?>("Blues"),
        });

        status.Should().Be(TagWriteStatus.Written);
        MediaFileMetadata after = await _reader.ReadAsync(path, MediaFileKind.Audio);
        after.Tags.Should().BeEquivalentTo(before.Tags with { Genre = "Blues", Genres = "Blues" });
        after.Duration.Should().Be(before.Duration);
        after.HasEmbeddedPicture.Should().BeTrue();
        EmbeddedPictureData? pictureAfter = new TagLibEmbeddedPictureReader().ReadPicture(path);
        pictureAfter.Should().NotBeNull();
        pictureAfter!.Data.Should().Equal(pictureBefore.Data);
        pictureAfter.MimeType.Should().Be(pictureBefore.MimeType);
    }

    [Test]
    public async Task Write_TrackNumber_KeepsTheTrackCount()
    {
        string path = _directory.Combine("song.mp3");
        await TestMediaGenerator.CreateAudioAsync(path, AudioFormat.Mp3, _tags);

        _writer.Write(path, MediaFileKind.Audio, new TagChanges
        {
            TrackNumber = new TagChange<int?>(5),
            DiscNumber = new TagChange<int?>(2),
        }).Should().Be(TagWriteStatus.Written);

        TagSnapshot snapshot = ReadSnapshot(path);
        (snapshot.TrackNumber, snapshot.TrackCount, snapshot.DiscNumber, snapshot.DiscCount)
            .Should().Be((5, 12, 2, 2));
    }

    [TestCase(MediaFileKind.Video)]
    [TestCase(MediaFileKind.Image)]
    public async Task Write_NoAudio_ReturnsNotAudioAndLeavesTheFileUnchanged(MediaFileKind kind)
    {
        string path = _directory.Combine("clip.mkv");
        await TestMediaGenerator.CreateVideoAsync(path, VideoFormat.Mkv, 320, 240, _tags);
        byte[] hash = Hash(path);
        DateTime modified = File.GetLastWriteTimeUtc(path);

        TagWriteStatus status = _writer.Write(path, kind, _allFields);

        status.Should().Be(TagWriteStatus.NotAudio);
        Hash(path).Should().Equal(hash);
        File.GetLastWriteTimeUtc(path).Should().Be(modified);
    }

    [Test]
    public async Task Write_ReadOnlyFile_ReturnsNotWritable()
    {
        string path = _directory.Combine("locked.mp3");
        await TestMediaGenerator.CreateAudioAsync(path, AudioFormat.Mp3, _tags);
        new FileInfo(path).IsReadOnly = true;
        byte[] hash = Hash(path);

        TagWriteStatus status = _writer.Write(path, MediaFileKind.Audio, _allFields);

        status.Should().Be(TagWriteStatus.NotWritable);
        Hash(path).Should().Equal(hash);
    }

    [Test]
    public void Write_MissingFile_ReturnsNotWritable()
    {
        TagWriteStatus status = _writer.Write(_directory.Combine("missing.mp3"), MediaFileKind.Audio, _allFields);

        status.Should().Be(TagWriteStatus.NotWritable);
    }

    [Test]
    public void Write_CorruptFile_ReturnsUnreadableOrUnsupported()
    {
        string path = _directory.Combine("broken.mp3");
        TestMediaGenerator.CreateCorruptFile(path);
        byte[] hash = Hash(path);

        TagWriteStatus status = _writer.Write(path, MediaFileKind.Audio, _allFields);

        status.Should().BeOneOf(TagWriteStatus.Unreadable, TagWriteStatus.Unsupported);
        Hash(path).Should().Equal(hash);
    }

    [Test]
    public void Write_UnknownFormat_ReturnsUnsupported()
    {
        string path = _directory.Combine("song.xyz");
        File.WriteAllBytes(path, [1, 2, 3, 4]);

        TagWriteStatus status = _writer.Write(path, MediaFileKind.Audio, _allFields);

        status.Should().Be(TagWriteStatus.Unsupported);
    }

    [Test]
    public async Task Write_NoChanges_DoesNotTouchTheFile()
    {
        string path = _directory.Combine("song.mp3");
        await TestMediaGenerator.CreateAudioAsync(path, AudioFormat.Mp3, _tags);
        byte[] hash = Hash(path);
        DateTime modified = File.GetLastWriteTimeUtc(path);

        TagWriteStatus status = _writer.Write(path, MediaFileKind.Audio, TagChanges.None);

        status.Should().Be(TagWriteStatus.Written);
        Hash(path).Should().Equal(hash);
        File.GetLastWriteTimeUtc(path).Should().Be(modified);
    }

    [TestCase(AudioFormat.Mp3, "mp3")]
    [TestCase(AudioFormat.Flac, "flac")]
    public async Task ReadSnapshot_TaggedFile_ReturnsTheRawValues(AudioFormat format, string extension)
    {
        string path = _directory.Combine($"song.{extension}");
        await TestMediaGenerator.CreateAudioAsync(path, format, _tags with { Title = "  Spaced   Title " });

        TagSnapshotResult result = _writer.ReadSnapshot(path);

        result.IsRead.Should().BeTrue();
        result.Snapshot.Should().BeEquivalentTo(new TagSnapshot(
            "  Spaced   Title ",
            ["Old Artist"],
            ["Old Album Artist"],
            "Old Album",
            2001,
            3,
            12,
            1,
            2,
            ["Rock"]));
    }

    [Test]
    public async Task ReadSnapshot_FileWithoutTags_ReturnsEmptyValues()
    {
        string path = _directory.Combine("plain.mp3");
        await TestMediaGenerator.CreateAudioAsync(path, AudioFormat.Mp3);

        TagSnapshotResult result = _writer.ReadSnapshot(path);

        result.IsRead.Should().BeTrue();
        result.Snapshot.Should().BeEquivalentTo(new TagSnapshot(null, [], [], null, null, null, null, null, null, []));
    }

    [Test]
    public void ReadSnapshot_CorruptFile_Fails()
    {
        // Random bytes may pass as an MPEG file without tags; a FLAC file needs its signature.
        string path = _directory.Combine("broken.flac");
        TestMediaGenerator.CreateCorruptFile(path);

        TagSnapshotResult result = _writer.ReadSnapshot(path);

        result.IsRead.Should().BeFalse();
        result.Status.Should().BeOneOf(TagReadStatus.Unreadable, TagReadStatus.Unsupported);
        result.Snapshot.Should().BeNull();
    }

    [Test]
    public void TagChanges_None_IsEmpty()
    {
        TagChanges.None.IsEmpty.Should().BeTrue();
        new TagChanges { Year = new TagChange<int?>(null) }.IsEmpty.Should().BeFalse();
    }

    private TagSnapshot ReadSnapshot(string path)
    {
        TagSnapshotResult result = _writer.ReadSnapshot(path);
        result.IsRead.Should().BeTrue();
        return result.Snapshot!;
    }

    private static byte[] Hash(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return SHA256.HashData(stream);
    }

    private static FfprobeMediaProber CreateProber()
    {
        return new FfprobeMediaProber(
            Microsoft.Extensions.Options.Options.Create(new MetadataOptions { FfprobePath = FfmpegTool.FfprobePath }),
            NullLogger<FfprobeMediaProber>.Instance);
    }
}
