using System.Globalization;
using System.Text.RegularExpressions;

namespace WaveMP.Core.Metadata.Paths;

/// <summary>
/// Recognizes folders of one disc of an album, e.g. <c>CD1</c>, <c>CD 2</c>, <c>Disc 1</c> or <c>disk-02</c>.
/// </summary>
public static partial class DiscFolders
{
    public static bool TryParse(string? folderName, out int discNumber)
    {
        discNumber = 0;
        Match match = DiscPattern().Match(folderName?.Trim() ?? string.Empty);
        return match.Success
            && int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out discNumber)
            && discNumber > 0;
    }

    [GeneratedRegex(@"^(?:cd|disc|disk)[\s._-]*(\d{1,2})$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DiscPattern();
}
