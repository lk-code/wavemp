using System.Buffers;
using System.Text;
using System.Text.RegularExpressions;

namespace WaveMP.Core.Playlists.Names;

/// <summary>
/// Rules for names of playlists: Unicode NFC, trimmed, inner whitespace collapsed to one space, 1 to 100 characters
/// and no control characters. Names do not have to be unique.
/// </summary>
public static partial class PlaylistNames
{
    public const int MaxLength = 100;

    /// <summary>
    /// Normalizes a name entered by a user. Returns <c>false</c> if the name is not valid.
    /// </summary>
    public static bool TryNormalize(string? input, out string name)
    {
        name = string.Empty;
        if (input is null || !IsValidUtf16(input))
        {
            // E.g. a lone surrogate, which cannot be stored as UTF-8.
            return false;
        }

        string unicode;
        try
        {
            unicode = input.Normalize(NormalizationForm.FormC);
        }
        catch (ArgumentException)
        {
            return false;
        }

        string normalized = WhitespaceRegex().Replace(unicode.Trim(), " ");
        if (normalized.Length is 0 or > MaxLength || normalized.Any(char.IsControl))
        {
            return false;
        }

        name = normalized;
        return true;
    }

    /// <summary>
    /// The form of a normalized name that playlists are sorted by: the same for every spelling in another case.
    /// </summary>
    public static string ToNormalized(string name)
    {
        return name.ToUpperInvariant();
    }

    private static bool IsValidUtf16(ReadOnlySpan<char> text)
    {
        while (!text.IsEmpty)
        {
            if (Rune.DecodeFromUtf16(text, out _, out int consumed) != OperationStatus.Done)
            {
                return false;
            }

            text = text[consumed..];
        }

        return true;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
