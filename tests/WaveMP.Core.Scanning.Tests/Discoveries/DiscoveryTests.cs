using AwesomeAssertions;
using WaveMP.Core.Scanning.Discoveries;
using WaveMP.Core.Settings.Groups;
using WaveMP.Data.Entities;
using WaveMP.Testing.Files;

namespace WaveMP.Core.Scanning.Tests.Discoveries;

[TestFixture]
public class DiscoveryTests
{
    [TestCase("Song.mp3", MediaFileKind.Audio)]
    [TestCase("SONG.FLAC", MediaFileKind.Audio)]
    [TestCase("Clip.mkv", MediaFileKind.Video)]
    [TestCase("cover.JPG", MediaFileKind.Image)]
    [TestCase("notes.txt", null)]
    [TestCase("README", null)]
    [TestCase(".mp3", null)]
    public void Classify_FileName_UsesTheExtensionLists(string fileName, MediaFileKind? expected)
    {
        MediaKindClassifier classifier = new(new LibrarySettings());

        classifier.Classify(fileName).Should().Be(expected);
    }

    [Test]
    public void Classify_WithChangedLists_FollowsTheSettings()
    {
        MediaKindClassifier classifier = new(new LibrarySettings
        {
            AudioExtensions = ["mp4"],
            VideoExtensions = ["mkv"],
            ImageExtensions = [],
        });

        classifier.Classify("a.mp4").Should().Be(MediaFileKind.Audio);
        classifier.Classify("a.jpg").Should().BeNull();
    }

    [TestCase(".hidden", true)]
    [TestCase("@eaDir", true)]
    [TestCase("#recycle", true)]
    [TestCase("#snapshot", true)]
    [TestCase("$RECYCLE.BIN", true)]
    [TestCase("System Volume Information", true)]
    [TestCase("lost+found", true)]
    [TestCase("Artist", false)]
    [TestCase("#1 Hits", false)]
    public void IsIgnored_Name_SkipsHiddenAndSystemFolders(string name, bool expected)
    {
        IgnoredNames.IsIgnored(name).Should().Be(expected);
    }

    [Test]
    public void TruncateToMicroseconds_Time_DropsTheTicksBelowAMicrosecond()
    {
        DateTimeOffset time = new(638_000_000_000_000_007, TimeSpan.Zero);

        MediaFolderWalker.TruncateToMicroseconds(time).UtcTicks.Should().Be(638_000_000_000_000_000);
    }

    [Test]
    public void Walk_MediaFolder_ListsTheMediaFilesFolderByFolderAndSkipsIgnoredOnes()
    {
        using TemporaryDirectory directory = new();
        Touch(directory, "Artist/Album/01 Song.mp3");
        Touch(directory, "Artist/Album/cover.jpg");
        Touch(directory, "Artist/Album/notes.txt");
        Touch(directory, "Artist/Album/.hidden.mp3");
        Touch(directory, "Artist/@eaDir/thumb.jpg");
        Touch(directory, ".trash/Song.mp3");
        Touch(directory, "Loose.mp3");
        List<string> tooLong = [];

        List<DiscoveredDirectory> result = [.. MediaFolderWalker.Walk(
            directory.Path,
            string.Empty,
            new MediaKindClassifier(new LibrarySettings()),
            2000,
            tooLong.Add,
            CancellationToken.None)];

        result.Select(item => item.RelativePath).Should().Equal(string.Empty, "Artist", "Artist/Album");
        result[0].Files.Select(file => file.RelativePath).Should().Equal("Loose.mp3");
        result[2].Files.Select(file => (file.RelativePath, file.Kind)).Should().Equal(
            ("Artist/Album/01 Song.mp3", MediaFileKind.Audio),
            ("Artist/Album/cover.jpg", MediaFileKind.Image));
        result[2].Files[0].Size.Should().Be(3);
        result[2].Files[0].ModifiedAt.UtcTicks.Should().Be(result[2].Files[0].ModifiedAt.UtcTicks / 10 * 10);
        tooLong.Should().BeEmpty();
    }

    [Test]
    public void Walk_PathLongerThanTheLimit_SkipsTheFileAndReportsIt()
    {
        using TemporaryDirectory directory = new();
        Touch(directory, "Artist/a-rather-long-name.mp3");
        Touch(directory, "Artist/b.mp3");
        List<string> tooLong = [];

        List<DiscoveredDirectory> result = [.. MediaFolderWalker.Walk(
            directory.Path,
            "Artist",
            new MediaKindClassifier(new LibrarySettings()),
            12,
            tooLong.Add,
            CancellationToken.None)];

        result.Single().Files.Select(file => file.RelativePath).Should().Equal("Artist/b.mp3");
        tooLong.Should().Equal("Artist/a-rather-long-name.mp3");
    }

    [Test]
    public void Walk_MissingFolder_FindsNothing()
    {
        using TemporaryDirectory directory = new();

        MediaFolderWalker.Walk(
                directory.Path,
                "Gone",
                new MediaKindClassifier(new LibrarySettings()),
                2000,
                _ => { },
                CancellationToken.None)
            .Should().BeEmpty();
    }

    private static void Touch(TemporaryDirectory directory, string relativePath)
    {
        string path = directory.Combine(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "abc");
    }
}
