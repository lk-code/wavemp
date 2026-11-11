using System.Text;

namespace WaveMP.Core.Metadata.Texts;

/// <summary>
/// Cleans texts from tags and file names and derives the keys that names are compared by.
/// </summary>
public static class MetadataText
{
    /// <summary>
    /// Longest stored name or title; longer texts are cut.
    /// </summary>
    public const int MaxLength = 512;

    /// <summary>
    /// Trims the text, joins white space to single spaces, removes control characters, normalizes it to NFC and cuts
    /// it to <see cref="MaxLength"/>. Returns <c>null</c> for <c>null</c> and texts without content.
    /// </summary>
    public static string? Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        StringBuilder builder = new(text.Length);
        bool space = false;
        foreach (char character in text.Normalize(NormalizationForm.FormC))
        {
            if (char.IsWhiteSpace(character) || char.IsControl(character))
            {
                space = builder.Length > 0;
                continue;
            }

            if (space)
            {
                builder.Append(' ');
                space = false;
            }

            builder.Append(character);
        }

        if (builder.Length == 0)
        {
            return null;
        }

        if (builder.Length > MaxLength)
        {
            int length = char.IsHighSurrogate(builder[MaxLength - 1]) ? MaxLength - 1 : MaxLength;
            builder.Length = length;
        }

        return builder.ToString().TrimEnd();
    }

    /// <summary>
    /// The key that names are compared by: <see cref="Normalize"/> in upper case.
    /// </summary>
    public static string? ToKey(string? text)
    {
        return Normalize(text)?.ToUpperInvariant();
    }
}
