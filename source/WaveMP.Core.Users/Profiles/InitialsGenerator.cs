using System.Buffers;
using System.Globalization;
using System.Text;

namespace WaveMP.Core.Users.Profiles;

/// <summary>
/// Derives the initials of a profile from its name: the first letter or digit of the first and of the last word
/// ("Lars Krämer" becomes "LK"), of the only word ("Anna" becomes "A"), or, if no word contains a letter or digit,
/// the first character of the name. Works on text elements, so accents, emoji and other scripts stay intact.
/// The result always follows the rules of <see cref="ProfileInitials"/>. If the text elements are too long for them
/// (e.g. a letter with many combining marks), only the first one is used, and if that is still too long, only the
/// first Unicode scalar of each text element.
/// </summary>
public static class InitialsGenerator
{
    /// <param name="name">A name normalized with <see cref="ProfileNames.TryNormalize"/>.</param>
    /// <returns>Initials that are valid under <see cref="ProfileInitials"/> and normalized by it.</returns>
    public static string FromName(string name)
    {
        string[] letters = [.. name
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Select(FirstLetterOrDigit)
            .OfType<string>()];

        string[] elements = letters.Length switch
        {
            0 => [FirstTextElement(name)],
            1 => [letters[0]],
            _ => [letters[0], letters[^1]],
        };

        foreach (string candidate in Candidates(elements))
        {
            if (ProfileInitials.TryNormalize(candidate.ToUpperInvariant(), out string initials))
            {
                return initials;
            }
        }

        // Only reached for a name that is not normalized, e.g. an empty one.
        return string.Empty;
    }

    /// <summary>
    /// The possible initials, from the most to the least complete one.
    /// </summary>
    private static IEnumerable<string> Candidates(string[] elements)
    {
        yield return string.Concat(elements);
        yield return elements[0];

        string[] scalars = [.. elements.Select(FirstScalar)];
        yield return string.Concat(scalars);
        yield return scalars[0];
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

    /// <summary>
    /// The base character of a text element without its combining marks or joined characters.
    /// </summary>
    private static string FirstScalar(string element)
    {
        return Rune.DecodeFromUtf16(element, out Rune rune, out _) == OperationStatus.Done
            ? rune.ToString()
            : string.Empty;
    }
}
