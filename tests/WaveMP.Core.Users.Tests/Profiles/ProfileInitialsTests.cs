using AwesomeAssertions;
using WaveMP.Core.Users.Profiles;

namespace WaveMP.Core.Users.Tests.Profiles;

[TestFixture]
public class ProfileInitialsTests
{
    [TestCase("L", "L")]
    [TestCase("lk", "lk")]
    [TestCase(" ABC ", "ABC")]
    [TestCase("\U0001F3B5", "\U0001F3B5")]
    [TestCase("\U0001F468‍\U0001F469‍\U0001F467", "\U0001F468‍\U0001F469‍\U0001F467")]
    [TestCase("李小龙", "李小龙")]
    public void TryNormalize_WithOneToThreeCharacters_KeepsThem(string input, string expected)
    {
        bool result = ProfileInitials.TryNormalize(input, out string initials);

        result.Should().BeTrue();
        initials.Should().Be(expected);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("  ")]
    [TestCase("ABCD")]
    [TestCase("A B")]
    [TestCase("A\u0001")]
    public void TryNormalize_WithInvalidInitials_ReturnsFalse(string? input)
    {
        bool result = ProfileInitials.TryNormalize(input, out _);

        result.Should().BeFalse();
    }

    [Test]
    public void TryNormalize_WithMoreThanSixteenCodeUnits_ReturnsFalse()
    {
        // Three family emoji are three text elements but 24 UTF-16 code units.
        string family = "\U0001F468‍\U0001F469‍\U0001F467";

        bool result = ProfileInitials.TryNormalize(family + family + family, out _);

        result.Should().BeFalse();
    }
}
