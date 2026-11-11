using AwesomeAssertions;
using WaveMP.Core.Metadata.Files;
using WaveMP.Core.Metadata.Options;
using WaveMP.Testing.Files;

namespace WaveMP.Core.Metadata.Tests.Files;

[TestFixture]
public class MediaFileLocatorTests
{
    private TemporaryDirectory _root = null!;
    private MediaFileLocator _locator = null!;

    [SetUp]
    public void SetUp()
    {
        _root = new TemporaryDirectory();
        string media = _root.CreateDirectory("media");
        _locator = new MediaFileLocator(Microsoft.Extensions.Options.Options.Create(
            new MediaLibraryOptions { RootPath = media + Path.DirectorySeparatorChar }));
    }

    [TearDown]
    public void TearDown()
    {
        _root.Dispose();
    }

    [TestCase("Song.mp3", "Song.mp3")]
    [TestCase("Band/Album/01 - One.flac", "Band/Album/01 - One.flac")]
    [TestCase("Band/./Album/cover.jpg", "Band/Album/cover.jpg")]
    [TestCase("Band/Other/../Album/cover.jpg", "Band/Album/cover.jpg")]
    public void TryGetFullPath_PathInsideTheMediaFolder_ReturnsTheAbsolutePath(string relativePath, string expected)
    {
        bool found = _locator.TryGetFullPath(relativePath, out string fullPath);

        found.Should().BeTrue();
        fullPath.Should().Be(_root.Combine("media/" + expected));
    }

    [TestCase("")]
    [TestCase("..")]
    [TestCase("../secret.txt")]
    [TestCase("Band/../../secret.txt")]
    [TestCase("../media-other/Song.mp3")]
    [TestCase(".")]
    [TestCase("Song\0.mp3")]
    public void TryGetFullPath_PathOutsideTheMediaFolder_ReturnsFalse(string relativePath)
    {
        bool found = _locator.TryGetFullPath(relativePath, out string fullPath);

        found.Should().BeFalse();
        fullPath.Should().BeEmpty();
    }

    [Test]
    public void TryGetFullPath_RootedPath_ReturnsFalse()
    {
        string rooted = _root.Combine("media/Song.mp3");

        bool found = _locator.TryGetFullPath(rooted, out _);

        found.Should().BeFalse();
        _locator.TryGetFullPath("/etc/passwd", out _).Should().BeFalse();
    }

    [Test]
    public void TryGetFullPath_FolderWithTheSamePrefix_ReturnsFalse()
    {
        // "media-other" starts with the name of the media folder but lies next to it.
        _root.CreateDirectory("media-other");

        bool found = _locator.TryGetFullPath("../media-other/Song.mp3", out _);

        found.Should().BeFalse();
    }
}
