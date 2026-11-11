using System.Globalization;
using AwesomeAssertions;
using WaveMP.Core.Users.Profiles;

namespace WaveMP.Core.Users.Tests.Profiles;

[TestFixture]
public class ProfileColorsTests
{
    [Test]
    public void Palette_Always_HasEightDistinctLowerCaseColors()
    {
        ProfileColors.Palette.Should().HaveCount(8);
        ProfileColors.Palette.Should().OnlyHaveUniqueItems();
        ProfileColors.Palette.Should().AllSatisfy(color => color.Should().MatchRegex("^#[0-9a-f]{6}$"));
    }

    [Test]
    public void Palette_Always_HasEnoughContrastToWhiteInitials()
    {
        // WCAG 2 contrast ratio between white text and the color, at least 4.5:1 for normal text.
        ProfileColors.Palette.Should().AllSatisfy(color =>
            ContrastToWhite(color).Should().BeGreaterThanOrEqualTo(4.5, color));
    }

    [TestCase("#1f6fd6", true)]
    [TestCase("#1F6FD6", true)]
    [TestCase("1f6fd6", false)]
    [TestCase("#1f6fd", false)]
    [TestCase("#1f6fd6a", false)]
    [TestCase("#gggggg", false)]
    [TestCase("#1f6fd6\n", false)]
    [TestCase(null, false)]
    public void IsValid_WithColor_AcceptsOnlyHexColors(string? color, bool expected)
    {
        bool result = ProfileColors.IsValid(color);

        result.Should().Be(expected);
    }

    [Test]
    public void Normalize_WithUpperCase_ReturnsLowerCase()
    {
        string result = ProfileColors.Normalize("#1F6FD6");

        result.Should().Be("#1f6fd6");
    }

    [Test]
    public void PickDefault_WithoutUsedColors_ReturnsFirstPaletteColor()
    {
        string result = ProfileColors.PickDefault([]);

        result.Should().Be(ProfileColors.Palette[0]);
    }

    [Test]
    public void PickDefault_WithUsedColors_ReturnsLeastUsedColor()
    {
        string[] used = [.. ProfileColors.Palette, .. ProfileColors.Palette.Where(color => color != "#6a3fb5")];

        string result = ProfileColors.PickDefault(used);

        result.Should().Be("#6a3fb5");
    }

    [Test]
    public void PickDefault_WithTie_ReturnsFirstInPaletteOrder()
    {
        string result = ProfileColors.PickDefault(["#1F6FD6", "#0b7a8a", "#123456"]);

        result.Should().Be("#2e7d32");
    }

    private static double ContrastToWhite(string color)
    {
        double luminance = (0.2126 * Channel(color, 1)) + (0.7152 * Channel(color, 3)) + (0.0722 * Channel(color, 5));
        return 1.05 / (luminance + 0.05);
    }

    private static double Channel(string color, int start)
    {
        double value = int.Parse(color.AsSpan(start, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0;
        return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
    }
}
