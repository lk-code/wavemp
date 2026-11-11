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
        // "e" followed by a combining acute accent is one character for the user.
        string result = InitialsGenerator.FromName("émile Zola");

        result.Should().Be("ÉZ");
    }
}
