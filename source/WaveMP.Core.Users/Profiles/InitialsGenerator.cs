using System.Globalization;
using System.Text;

namespace WaveMP.Core.Users.Profiles;

/// <summary>
/// Derives the initials of a profile from its name: the first letter or digit of the first and of the last word
/// ("Lars Krämer" becomes "LK"), of the only word ("Anna" becomes "A"), or, if no word contains a letter or digit,
/// the first character of the name. Works on text elements, so accents, emoji and other scripts stay intact.
/// </summary>
public static class InitialsGenerator
{
    /// <param name="name">A name normalized with <see cref="ProfileNames.TryNormalize"/>.</param>
    public static string FromName(string name)
    {
        string[] initials = [.. name
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Select(FirstLetterOrDigit)
            .OfType<string>()];

        string result = initials.Length switch
        {
            0 => FirstTextElement(name),
            1 => initials[0],
            _ => initials[0] + initials[^1],
        };

        return result.ToUpperInvariant();
    }

    private static string? FirstLetterOrDigit(string word)
    {
        TextElementEnumerator elements = StringInfo.GetTextElementEnumerator(word);
        while (elements.MoveNext())
        {
            string element = elements.GetTextElement();
            if (Rune.TryGetRuneAt(element, 0, out Rune rune) && Rune.IsLetterOrDigit(rune))
            {
                return element;
            }
        }

        return null;
    }

    private static string FirstTextElement(string name)
    {
        TextElementEnumerator elements = StringInfo.GetTextElementEnumerator(name.Trim());
        return elements.MoveNext() ? elements.GetTextElement() : string.Empty;
    }
}
