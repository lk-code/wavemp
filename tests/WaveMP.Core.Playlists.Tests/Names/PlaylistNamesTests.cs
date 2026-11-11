using AwesomeAssertions;
using WaveMP.Core.Playlists.Names;

namespace WaveMP.Core.Playlists.Tests.Names;

[TestFixture]
public class PlaylistNamesTests
{
    [TestCase("Road Trip", "Road Trip")]
    [TestCase("  Road   Trip \t", "Road Trip")]
    [TestCase("Line\nBreak", "Line Break")]
    public void TryNormalize_ValidName_TrimsAndCollapsesWhitespace(string input, string expected)
    {
        PlaylistNames.TryNormalize(input, out string name).Should().BeTrue();

        name.Should().Be(expected);
    }

    [Test]
    public void TryNormalize_DecomposedAccents_ComposesThem()
    {
        const string Decomposed = "Café Mélange";

        PlaylistNames.TryNormalize(Decomposed, out string name).Should().BeTrue();

        name.Should().Be("Café Mélange");
        PlaylistNames.TryNormalize("Café Mélange", out string precomposed).Should().BeTrue();
        precomposed.Should().Be(name);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("Bell\u0007")]
    public void TryNormalize_InvalidName_ReturnsFalse(string? input)
    {
        PlaylistNames.TryNormalize(input, out _).Should().BeFalse();
    }

    [Test]
    public void TryNormalize_LoneSurrogate_ReturnsFalse()
    {
        // Built at runtime: attribute arguments cannot carry a lone surrogate.
        string input = "Lone " + (char)0xD800 + " surrogate";

        PlaylistNames.TryNormalize(input, out _).Should().BeFalse();
    }

    [Test]
    public void TryNormalize_LongName_AcceptsTheMaximumOnly()
    {
        PlaylistNames.TryNormalize(new string('a', PlaylistNames.MaxLength), out _).Should().BeTrue();
        PlaylistNames.TryNormalize(new string('a', PlaylistNames.MaxLength + 1), out _).Should().BeFalse();
    }

    [Test]
    public void ToNormalized_Always_IgnoresTheCase()
    {
        PlaylistNames.ToNormalized("Road Trip").Should().Be(PlaylistNames.ToNormalized("ROAD trip"));
    }
}
