using AwesomeAssertions;
using WaveMP.Core.Users.Profiles;

namespace WaveMP.Core.Users.Tests.Profiles;

[TestFixture]
public class InitialsGeneratorTests
{
    [TestCase("Lars Krämer", "LK")]
    [TestCase("anna", "A")]
    [TestCase("Admin", "A")]
    [TestCase("Anna-Lena Schmidt", "AS")]
    [TestCase("Max von Mustermann", "MM")]
    [TestCase("élodie durand", "ÉD")]
    [TestCase("\U0001F3B5 Fan", "F")]
    [TestCase("\U0001F3B5", "\U0001F3B5")]
    [TestCase("李小龙", "李")]
    [TestCase("r2d2", "R")]
    [TestCase("(DJ) Nova", "DN")]
    public void FromName_WithName_ReturnsInitialsOfFirstAndLastWord(string name, string expected)
    {
        string result = InitialsGenerator.FromName(name);

        result.Should().Be(expected);
    }

    [Test]
    public void FromName_WithCombiningCharacters_KeepsTheTextElement()
    {
        // "q" followed by a combining dot below is one character for the user and has no precomposed form.
        string result = InitialsGenerator.FromName("q\u0323uinn Zola");

        result.Should().Be("Q\u0323Z");
    }

    [Test]
    public void FromName_WithDecomposedCharacters_ReturnsThemComposed()
    {
        // Initials are normalized like entered ones: "E" with a combining acute accent becomes "É".
        string result = InitialsGenerator.FromName("e\u0301mile Zola");

        result.Should().Be("\u00C9Z");
    }

    [Test]
    public void FromName_WithTooLongTextElement_UsesItsBaseCharacter()
    {
        // The first word starts with "á" and 17 more combining marks: one text element of 18 UTF-16 code units.
        string name = Normalize(LetterWithMarks('a', 18) + " x");

        string result = InitialsGenerator.FromName(name);

        result.Should().Be("\u00C1X");
    }

    [Test]
    public void FromName_WithTextElementsTooLongTogether_UsesTheFirstOnly()
    {
        // Two text elements of 10 UTF-16 code units each: valid alone, too long together.
        string first = LetterWithMarks('b', 9);
        string name = Normalize(first + " " + LetterWithMarks('d', 9));

        string result = InitialsGenerator.FromName(name);

        result.Should().Be(first.ToUpperInvariant());
    }

    [Test]
    public void FromName_WithoutLettersAndWithTooLongTextElement_UsesItsBaseCharacter()
    {
        string name = Normalize(LetterWithMarks('!', 20));

        string result = InitialsGenerator.FromName(name);

        result.Should().Be("!");
    }

    private static IEnumerable<TestCaseData> ValidNames()
    {
        const string Family = "\U0001F468\u200D\U0001F469\u200D\U0001F467";
        yield return new TestCaseData("Lars Krämer").SetArgDisplayNames("Latin");
        yield return new TestCaseData(Family + " " + Family).SetArgDisplayNames("Emoji sequences");
        yield return new TestCaseData("\U0001F1E9\U0001F1EA").SetArgDisplayNames("Flag");
        yield return new TestCaseData("i\u0307nci").SetArgDisplayNames("Upper case composes");
        yield return new TestCaseData(LetterWithMarks('a', 18) + " x").SetArgDisplayNames("Long first");
        yield return new TestCaseData("x " + LetterWithMarks('z', 30)).SetArgDisplayNames("Long last");
        yield return new TestCaseData(LetterWithMarks('m', 12) + " " + LetterWithMarks('n', 12))
            .SetArgDisplayNames("Long together");
        yield return new TestCaseData(LetterWithMarks('-', 40)).SetArgDisplayNames("Long without letters");
    }

    [TestCaseSource(nameof(ValidNames))]
    public void FromName_WithAnyValidName_ReturnsValidInitials(string input)
    {
        string name = Normalize(input);

        string result = InitialsGenerator.FromName(name);

        ProfileInitials.TryNormalize(result, out string normalized).Should().BeTrue(result);
        normalized.Should().Be(result);
        result.Length.Should().BeLessThanOrEqualTo(ProfileInitials.MaxLength);
    }

    /// <summary>
    /// The character followed by different combining marks from U+0301 on, as one text element.
    /// </summary>
    private static string LetterWithMarks(char character, int marks)
    {
        return character + string.Concat(Enumerable.Range(0x0301, marks).Select(mark => (char)mark));
    }

    private static string Normalize(string input)
    {
        ProfileNames.TryNormalize(input, out string name).Should().BeTrue(input);
        return name;
    }
}
