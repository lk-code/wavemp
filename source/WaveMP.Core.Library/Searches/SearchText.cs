using WaveMP.Core.Metadata.Texts;

namespace WaveMP.Core.Library.Searches;

/// <summary>
/// Cleans search texts like the names they are compared with.
/// </summary>
public static class SearchText
{
    /// <summary>
    /// Longest search text that is accepted.
    /// </summary>
    public const int MaxLength = 200;

    /// <summary>
    /// Trims the text, joins white space to single spaces, removes control characters, normalizes it to NFC and cuts it
    /// to <see cref="MaxLength"/>. Returns <c>null</c> for texts without content, which search nothing.
    /// </summary>
    public static string? Normalize(string? text)
    {
        string? normalized = MetadataText.Normalize(text);
        if (normalized is null || normalized.Length <= MaxLength)
        {
            return normalized;
        }

        int length = char.IsHighSurrogate(normalized[MaxLength - 1]) ? MaxLength - 1 : MaxLength;
        return normalized[..length].TrimEnd();
    }
}
