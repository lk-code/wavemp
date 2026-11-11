using AwesomeAssertions;
using WaveMP.Core.Metadata.Paths;

namespace WaveMP.Core.Metadata.Tests.Paths;

[TestFixture]
public class FileNamePatternDetectorTests
{
    [Test]
    public void Detect_ArtistFirstAndTitles_FindsArtistAndTitle()
    {
        FileNamePattern pattern = FileNamePatternDetector.Detect(["A - X", "A - Y", "B - Z"])!;

        pattern.ToString().Should().Be("{Artist} - {Title}");
        pattern.Match("C - W").Should().Be(new FileNameFields(Artist: "C", Title: "W"));
    }

    [Test]
    public void Detect_RepeatingArtistLast_FindsTheArtistByItsRepetition()
    {
        FileNamePattern pattern = FileNamePatternDetector.Detect(["X - A", "Y - A", "Z - A", "W - B"])!;

        pattern.ToString().Should().Be("{Title} - {Artist}");
    }

    [Test]
    public void Detect_NumberArtistAndTitle_FindsAllThree()
    {
        FileNamePattern pattern = FileNamePatternDetector.Detect(["01 - A - X", "02 - B - Y", "03 - C - Z"])!;

        pattern.ToString().Should().Be("{TrackNumber} - {Artist} - {Title}");
        pattern.Match("04 - D - W").Should().Be(new FileNameFields(TrackNumber: 4, Artist: "D", Title: "W"));
    }

    [Test]
    public void Detect_NumberWithoutLeadingZero_IsATrackNumberOfThePattern()
    {
        FileNamePattern pattern = FileNamePatternDetector.Detect(["1 First", "2 Second", "3 Third"])!;

        pattern.Match("10 Tenth").Should().Be(new FileNameFields(TrackNumber: 10, Title: "Tenth"));
    }

    [Test]
    public void Detect_ThreeTexts_AreArtistAlbumAndTitle()
    {
        FileNamePattern pattern = FileNamePatternDetector.Detect(["A - P - X", "A - P - Y", "B - Q - Z"])!;

        pattern.ToString().Should().Be("{Artist} - {Album} - {Title}");
    }

    [Test]
    public void Detect_MostNamesShareAShape_IgnoresTheOthers()
    {
        string[] names = ["A - 1", "A - 2", "B - 3", "C - 4", "Single"];

        FileNamePattern pattern = FileNamePatternDetector.Detect(names)!;

        pattern.ToString().Should().Be("{Artist} - {Title}");
        pattern.Match("Single").Should().BeNull();
    }

    [Test]
    public void Detect_MixedNames_FindsNoPattern()
    {
        FileNamePatternDetector.Detect(["A - X", "Song", "01 Track", "B - C - D"]).Should().BeNull();
    }

    [Test]
    public void Detect_FewerThanThreeNames_FindsNoPattern()
    {
        FileNamePatternDetector.Detect(["A - X", "B - Y"]).Should().BeNull();
    }

    [Test]
    public void Detect_MoreThanThreeTexts_FindsNoPattern()
    {
        FileNamePatternDetector.Detect(["a - b - c - d", "e - f - g - h", "i - j - k - l"]).Should().BeNull();
    }
}
