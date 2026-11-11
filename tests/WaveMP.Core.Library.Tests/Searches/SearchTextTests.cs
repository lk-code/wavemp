using AwesomeAssertions;
using WaveMP.Core.Library.Searches;

namespace WaveMP.Core.Library.Tests.Searches;

[TestFixture]
public class SearchTextTests
{
    [TestCase(null, null)]
    [TestCase("", null)]
    [TestCase("   \t ", null)]
    [TestCase("  Alpha   Band ", "Alpha Band")]
    [TestCase("Line\nbreak", "Line break")]
    public void Normalize_Always_CleansTheText(string? text, string? expected)
    {
        SearchText.Normalize(text).Should().Be(expected);
    }

    [Test]
    public void Normalize_LongText_IsCutToTheMaximumLength()
    {
        string text = new('a', SearchText.MaxLength + 50);

        SearchText.Normalize(text).Should().HaveLength(SearchText.MaxLength);
    }

    [Test]
    public void Normalize_LongTextWithASurrogatePairAtTheEnd_KeepsThePairWhole()
    {
        string text = new string('a', SearchText.MaxLength - 1) + "\U0001F3B5" + "tail";

        string normalized = SearchText.Normalize(text)!;

        normalized.Should().HaveLength(SearchText.MaxLength - 1);
        char.IsHighSurrogate(normalized[^1]).Should().BeFalse();
    }
}
