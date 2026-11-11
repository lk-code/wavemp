using AwesomeAssertions;
using WaveMP.Data.Queries;

namespace WaveMP.Data.Tests.Queries;

[TestFixture]
public class LikePatternsTests
{
    [TestCase("plain", "plain")]
    [TestCase("100%", "100\\%")]
    [TestCase("a_b", "a\\_b")]
    [TestCase("back\\slash", "back\\\\slash")]
    [TestCase("\\%_", "\\\\\\%\\_")]
    public void Escape_Always_EscapesWildcardsAndTheEscapeCharacter(string text, string expected)
    {
        LikePatterns.Escape(text).Should().Be(expected);
    }

    [Test]
    public void Contains_Always_WrapsTheEscapedTextInWildcards()
    {
        LikePatterns.Contains("50%").Should().Be("%50\\%%");
    }
}
