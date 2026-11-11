using System.Globalization;
using System.Text.RegularExpressions;
using WaveMP.Core.Metadata.Texts;

namespace WaveMP.Core.Metadata.Paths;

/// <summary>
/// Splits a file name into a leading number and the texts between the separators <c>" - "</c>, <c>" – "</c> and
/// <c>" — "</c>. Names without spaces use underscores as spaces.
/// </summary>
internal static partial class FileNameTokens
{
    private static readonly string[] _separators = [" - ", " – ", " — "];

    /// <summary>
    /// The leading number of the name (1 to 3 digits, followed by a separator) and the rest.
    /// </summary>
    public static (string? Number, string Remainder) SplitNumber(string name)
    {
        Match match = LeadingNumberPattern().Match(name);
        return match.Success ? (match.Groups[1].Value, match.Groups[2].Value) : (null, name);
    }

    /// <summary>
    /// A leading disc and track number such as <c>1-03</c> or <c>2.11</c>, and the rest.
    /// </summary>
    public static (string Disc, string Track, string Remainder)? SplitDiscAndTrack(string name)
    {
        Match match = DiscAndTrackPattern().Match(name);
        return match.Success ? (match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value) : null;
    }

    /// <summary>
    /// The texts between the separators, normalized; empty texts are left out.
    /// </summary>
    public static string[] SplitTexts(string text)
    {
        string spaced = !text.Contains(' ', StringComparison.Ordinal) && text.Contains('_', StringComparison.Ordinal)
            ? text.Replace('_', ' ')
            : text;
        return [.. spaced
            .Split(_separators, StringSplitOptions.None)
            .Select(MetadataText.Normalize)
            .OfType<string>()];
    }

    public static string Clean(string name)
    {
        string spaced = !name.Contains(' ', StringComparison.Ordinal) && name.Contains('_', StringComparison.Ordinal)
            ? name.Replace('_', ' ')
            : name;
        return MetadataText.Normalize(spaced) ?? string.Empty;
    }

    public static int? ParseNumber(string? digits)
    {
        return int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out int number) && number > 0
            ? number
            : null;
    }

    [GeneratedRegex(@"^(\d{1,3})(?:\s*\.\s*|\s*-\s*|\s*_\s*|\s+)(\S.*)$", RegexOptions.CultureInvariant)]
    private static partial Regex LeadingNumberPattern();

    [GeneratedRegex(@"^(\d{1,2})[-.](\d{1,3})(?:\s*\.\s*|\s*-\s*|\s*_\s*|\s+)(\S.*)$", RegexOptions.CultureInvariant)]
    private static partial Regex DiscAndTrackPattern();
}
