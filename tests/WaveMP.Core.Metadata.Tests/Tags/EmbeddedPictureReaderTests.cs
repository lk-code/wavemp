using AwesomeAssertions;
using WaveMP.Core.Metadata.Tags;
using WaveMP.Testing.Files;
using WaveMP.Testing.Media;

namespace WaveMP.Core.Metadata.Tests.Tags;

/// <summary>
/// Reads pictures embedded into synthetic media files created with ffmpeg.
/// </summary>
[TestFixture]
[Category("Media")]
public class EmbeddedPictureReaderTests
{
    private static readonly byte[] _jpegSignature = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] _pngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private TemporaryDirectory _directory = null!;
    private TagLibEmbeddedPictureReader _reader = null!;

    [SetUp]
    public void SetUp()
    {
        _directory = new TemporaryDirectory();
        _reader = new TagLibEmbeddedPictureReader();
    }

    [TearDown]
    public void TearDown()
    {
        _directory.Dispose();
    }

    [TestCase(AudioFormat.Mp3, "mp3", "jpg")]
    [TestCase(AudioFormat.Flac, "flac", "png")]
    [TestCase(AudioFormat.M4a, "m4a", "jpg")]
    public async Task ReadPicture_AudioWithEmbeddedCover_ReturnsThePicture(
        AudioFormat format,
        string extension,
        string pictureExtension)
    {
        string cover = _directory.Combine($"cover.{pictureExtension}");
        await TestMediaGenerator.CreateImageAsync(cover, 64, 64);
        string path = _directory.Combine($"song.{extension}");
        await TestMediaGenerator.CreateAudioAsync(path, format, embeddedCoverPath: cover);

        EmbeddedPictureData? picture = _reader.ReadPicture(path);

        picture.Should().NotBeNull();
        if (pictureExtension == "png")
        {
            picture!.Data.Should().StartWith(_pngSignature);
            picture.MimeType.Should().Be("image/png");
        }
        else
        {
            picture!.Data.Should().StartWith(_jpegSignature);
            picture.MimeType.Should().Be("image/jpeg");
        }
    }

    [Test]
    public async Task ReadPicture_AudioWithoutPicture_ReturnsNull()
    {
        string path = _directory.Combine("plain.mp3");
        await TestMediaGenerator.CreateAudioAsync(path, AudioFormat.Mp3);

        EmbeddedPictureData? picture = _reader.ReadPicture(path);

        picture.Should().BeNull();
    }

    [Test]
    public void ReadPicture_DamagedFile_ReturnsNull()
    {
        string path = _directory.Combine("broken.mp3");
        TestMediaGenerator.CreateCorruptFile(path);

        EmbeddedPictureData? picture = _reader.ReadPicture(path);

        picture.Should().BeNull();
    }

    [Test]
    public void ReadPicture_MissingFile_ReturnsNull()
    {
        EmbeddedPictureData? picture = _reader.ReadPicture(_directory.Combine("missing/song.flac"));

        picture.Should().BeNull();
    }
}
