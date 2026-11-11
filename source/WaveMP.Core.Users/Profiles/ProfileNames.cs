using System.Buffers;
using System.Text;
using System.Text.RegularExpressions;

namespace WaveMP.Core.Users.Profiles;

/// <summary>
/// Rules for profile names: Unicode NFC, trimmed, inner whitespace collapsed to one space, 1 to 50 characters and no
/// control characters. Names are unique without regard to case.
/// </summary>
public static partial class ProfileNames
{
    public const int MaxLength = 50;

    /// <summary>
    /// Normalizes a name entered by a user. Returns <c>false</c> if the name is not valid.
    /// </summary>
    public static bool TryNormalize(string? input, out string name)
    {
        name = string.Empty;
        if (input is null || !TryNormalizeUnicode(input, out string unicode))
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
    /// The form of a normalized name that is stored for the unique index: the same for every spelling in another case.
    /// </summary>
    public static string ToNormalized(string name)
    {
        return name.ToUpperInvariant();
    }

    internal static bool TryNormalizeUnicode(string input, out string normalized)
    {
        normalized = string.Empty;
        if (!IsValidUtf16(input))
        {
            // E.g. a lone surrogate, which cannot be stored as UTF-8.
            return false;
        }

        try
        {
            normalized = input.Normalize(NormalizationForm.FormC);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
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
