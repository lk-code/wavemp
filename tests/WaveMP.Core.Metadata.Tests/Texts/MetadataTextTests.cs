using AwesomeAssertions;
using WaveMP.Core.Metadata.Texts;

namespace WaveMP.Core.Metadata.Tests.Texts;

[TestFixture]
public class MetadataTextTests
{
    [TestCase(null, null)]
    [TestCase("", null)]
    [TestCase("   ", null)]
    [TestCase("  Abbey   Road  ", "Abbey Road")]
    [TestCase("Line\nBreak\tTab", "Line Break Tab")]
    [TestCase("Bad\u0000Char", "Bad Char")]
    public void Normalize_Text_TrimsAndJoinsWhiteSpace(string? text, string? expected)
    {
        MetadataText.Normalize(text).Should().Be(expected);
    }

    [Test]
    public void Normalize_DecomposedText_IsComposed()
    {
        MetadataText.Normalize("Beyoncé").Should().Be("Beyoncé");
    }

    [Test]
    public void Normalize_LongText_IsCutToTheMaximumLength()
    {
        string text = new('a', MetadataText.MaxLength + 10);

        MetadataText.Normalize(text).Should().HaveLength(MetadataText.MaxLength);
    }

    [Test]
    public void Normalize_LongTextEndingInASurrogatePair_DoesNotSplitThePair()
    {
        string text = new string('a', MetadataText.MaxLength - 1) + "\U0001F3B5" + "tail";

        string normalized = MetadataText.Normalize(text)!;

        normalized.Should().HaveLength(MetadataText.MaxLength - 1);
        char.IsHighSurrogate(normalized[^1]).Should().BeFalse();
    }

    [TestCase(" Daft  punk ", "DAFT PUNK")]
    [TestCase("", null)]
    public void ToKey_Text_IsTheNormalizedUpperCaseText(string text, string? expected)
    {
        MetadataText.ToKey(text).Should().Be(expected);
    }
}
