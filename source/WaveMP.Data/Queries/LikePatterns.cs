namespace WaveMP.Data.Queries;

/// <summary>
/// Patterns for <c>LIKE</c> and <c>ILIKE</c> from texts of the user: the wildcards <c>%</c> and <c>_</c> and the
/// escape character in the text match themselves.
/// </summary>
public static class LikePatterns
{
    /// <summary>
    /// Escape character of the patterns.
    /// </summary>
    public const string EscapeCharacter = "\\";

    /// <summary>
    /// The text with every wildcard and escape character escaped, so the pattern matches exactly the text.
    /// </summary>
    public static string Escape(string text)
    {
        return text
            .Replace(EscapeCharacter, EscapeCharacter + EscapeCharacter, StringComparison.Ordinal)
            .Replace("%", EscapeCharacter + "%", StringComparison.Ordinal)
            .Replace("_", EscapeCharacter + "_", StringComparison.Ordinal);
    }

    /// <summary>
    /// A pattern that matches every text containing <paramref name="text"/>.
    /// </summary>
    public static string Contains(string text)
    {
        return "%" + Escape(text) + "%";
    }
}
