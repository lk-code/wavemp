using AwesomeAssertions;
using WaveMP.Core.Users.Profiles;

namespace WaveMP.Core.Users.Tests.Profiles;

[TestFixture]
public class ProfileNamesTests
{
    [TestCase("Lars", "Lars")]
    [TestCase("  Lars  ", "Lars")]
    [TestCase("Lars \t  Krämer", "Lars Krämer")]
    [TestCase("Lars\nKrämer", "Lars Krämer")]
    public void TryNormalize_WithValidName_TrimsAndCollapsesWhitespace(string input, string expected)
    {
        bool result = ProfileNames.TryNormalize(input, out string name);

        result.Should().BeTrue();
        name.Should().Be(expected);
    }

    [Test]
    public void TryNormalize_WithDecomposedCharacters_ComposesThem()
    {
        bool result = ProfileNames.TryNormalize("Krämer", out string name);

        result.Should().BeTrue();
        name.Should().Be("Krämer");
    }

    [Test]
    public void TryNormalize_WithCombiningAndPrecomposedAccent_ReturnsTheSamePrecomposedName()
    {
        ProfileNames.TryNormalize("José", out string combining).Should().BeTrue();
        ProfileNames.TryNormalize("José", out string precomposed).Should().BeTrue();

        combining.Should().Be("José");
        precomposed.Should().Be("José");
    }

    [TestCase("José", "josé")]
    [TestCase("JOSÉ", "José")]
    [TestCase("Renée Müller", "RENÉE MÜLLER")]
    public void ToNormalized_WithCombiningAndPrecomposedAccents_ReturnsTheSameValue(string first, string second)
    {
        ProfileNames.TryNormalize(first, out string firstName).Should().BeTrue();
        ProfileNames.TryNormalize(second, out string secondName).Should().BeTrue();

        ProfileNames.ToNormalized(firstName).Should().Be(ProfileNames.ToNormalized(secondName));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("Lars\u0001")]
    public void TryNormalize_WithInvalidName_ReturnsFalse(string? input)
    {
        bool result = ProfileNames.TryNormalize(input, out _);

        result.Should().BeFalse();
    }

    [Test]
    public void TryNormalize_WithLoneSurrogate_ReturnsFalse()
    {
        // Built at runtime: attribute arguments would replace the lone surrogate.
        string input = "Lars" + (char)0xD800;

        bool result = ProfileNames.TryNormalize(input, out _);

        result.Should().BeFalse();
    }

    [Test]
    public void TryNormalize_WithMaximumLength_Succeeds()
    {
        bool result = ProfileNames.TryNormalize(new string('a', ProfileNames.MaxLength), out _);

        result.Should().BeTrue();
    }

    [Test]
    public void TryNormalize_LongerThanMaximum_ReturnsFalse()
    {
        bool result = ProfileNames.TryNormalize(new string('a', ProfileNames.MaxLength + 1), out _);

        result.Should().BeFalse();
    }

    [Test]
    public void ToNormalized_WithDifferentCase_ReturnsTheSameValue()
    {
        string lower = ProfileNames.ToNormalized("lars krämer");
        string mixed = ProfileNames.ToNormalized("Lars KRÄMER");

        lower.Should().Be(mixed);
    }
}
