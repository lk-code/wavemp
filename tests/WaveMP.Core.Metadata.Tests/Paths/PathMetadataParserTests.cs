using AwesomeAssertions;
using WaveMP.Core.Metadata.Paths;

namespace WaveMP.Core.Metadata.Tests.Paths;

[TestFixture]
public class PathMetadataParserTests
{
    private static IEnumerable<TestCaseData> Paths()
    {
        yield return Case("Artist/Album/01 - Title", new("Title", true, "Artist", null, "Album", 1));
        yield return Case("Artist/Album/1 Title", new("Title", true, "Artist", null, "Album", 1));
        yield return Case("Artist/Album/07. Title", new("Title", true, "Artist", null, "Album", 7));
        yield return Case("Artist/Album/12_Title_Name", new("Title Name", true, "Artist", null, "Album", 12));
        yield return Case("Artist/Album/CD2/03 Title", new("Title", true, "Artist", null, "Album", 3, 2));
        yield return Case("Artist/Album/Disc 1/01 - Title", new("Title", true, "Artist", null, "Album", 1, 1));
        yield return Case("Artist/Album/1-05 Title", new("Title", true, "Artist", null, "Album", 5, 1));
        yield return Case("Artist/Song", new("Song", false, "Artist"));
        yield return Case("Artist - Album/02. Title", new("Title", true, "Artist", null, "Album", 2));
        yield return Case("Artist/2001 - Album/01 - Title", new("Title", true, "Artist", null, "Album", 1, null, 2001));
        yield return Case("Artist/(1999) Album/Title", new("Title", false, "Artist", null, "Album", null, null, 1999));
        yield return Case("Artist/[1999] Album/Title", new("Title", false, "Artist", null, "Album", null, null, 1999));
        yield return Case("Artist/Album (2005)/Title", new("Title", false, "Artist", null, "Album", null, null, 2005));
        yield return Case("Artist/Album (1234)/Title", new("Title", false, "Artist", null, "Album (1234)"));
        yield return Case("A - X", new("X", true, "A"));
        yield return Case("50 Cent - Song", new("Song", true, "50 Cent"));
        yield return Case("05 - Song", new("Song", true, null, null, null, 5));
        yield return Case("A - Album - X", new("X", true, "A", null, "Album"));
        yield return Case("Some_Title_Here", new("Some Title Here", false));
        yield return Case("weird", new("weird", false));
        yield return Case("Artist/Artist - Song", new("Song", true, "Artist"));
        yield return Case("Various/Hits/01 - A - X", new("X", true, "A", "Various", "Hits", 1));
        yield return Case("Music/Sub/Artist/Album/02 Title", new("Title", true, "Artist", null, "Album", 2));
    }

    [TestCaseSource(nameof(Paths))]
    public void Parse_PathWithoutPattern_DerivesTheMetadata(string relativePath, PathMetadata expected)
    {
        string[] segments = relativePath.Split('/');

        PathMetadata result = PathMetadataParser.Parse(segments[..^1], segments[^1]);

        result.Should().Be(expected);
    }

    [Test]
    public void Parse_WithFolderPattern_UsesThePattern()
    {
        FileNamePattern pattern = FileNamePatternDetector.Detect(["X - A", "Y - A", "Z - A"])!;

        PathMetadata result = PathMetadataParser.Parse([], "W - A", pattern);

        result.Should().Be(new PathMetadata("W", true, "A"));
    }

    [Test]
    public void Parse_NameThatDoesNotMatchThePattern_UsesTheRulesForSingleNames()
    {
        FileNamePattern pattern = FileNamePatternDetector.Detect(["01 - A - X", "02 - B - Y", "03 - C - Z"])!;

        PathMetadata result = PathMetadataParser.Parse([], "Loose Song", pattern);

        result.Should().Be(new PathMetadata("Loose Song", false));
    }

    [TestCase("CD1", 1)]
    [TestCase("cd 2", 2)]
    [TestCase("Disc 10", 10)]
    [TestCase("disk-02", 2)]
    [TestCase("Disc_3", 3)]
    public void DiscFolders_DiscFolder_IsRecognized(string name, int expected)
    {
        DiscFolders.TryParse(name, out int disc).Should().BeTrue();
        disc.Should().Be(expected);
    }

    [TestCase("CD")]
    [TestCase("Discography")]
    [TestCase("CD 123")]
    [TestCase("Album")]
    public void DiscFolders_OtherFolder_IsNoDiscFolder(string name)
    {
        DiscFolders.TryParse(name, out _).Should().BeFalse();
    }

    private static TestCaseData Case(string path, PathMetadata expected)
    {
        return new TestCaseData(path, expected).SetArgDisplayNames(path);
    }
}
