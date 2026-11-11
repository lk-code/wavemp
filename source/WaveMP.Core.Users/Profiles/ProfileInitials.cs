using System.Globalization;

namespace WaveMP.Core.Users.Profiles;

/// <summary>
/// Rules for initials entered by a user: 1 to 3 characters as perceived by the user (text elements, so an emoji
/// counts once), at most 16 UTF-16 code units, no whitespace and no control characters. The case is kept.
/// </summary>
public static class ProfileInitials
{
    public const int MaxTextElements = 3;
    public const int MaxLength = 16;

    public static bool TryNormalize(string? input, out string initials)
    {
        initials = string.Empty;
        if (input is null || !ProfileNames.TryNormalizeUnicode(input, out string unicode))
        {
            return false;
        }

        string trimmed = unicode.Trim();
        if (trimmed.Length is 0 or > MaxLength
            || trimmed.Any(character => char.IsWhiteSpace(character) || char.IsControl(character))
            || new StringInfo(trimmed).LengthInTextElements > MaxTextElements)
        {
            return false;
        }

        initials = trimmed;
        return true;
    }
}
