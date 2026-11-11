using System.Text.RegularExpressions;

namespace WaveMP.Core.Users.Profiles;

/// <summary>
/// Colors of profiles, stored as <c>#rrggbb</c> in lower case. The palette is used for the default color; all its
/// colors have a contrast of at least 4.5:1 to white initials.
/// </summary>
public static partial class ProfileColors
{
    /// <summary>
    /// Blue, teal, green, purple, pink, orange, brown and slate; the order is the priority for new profiles.
    /// </summary>
    public static IReadOnlyList<string> Palette { get; } =
    [
        "#1f6fd6",
        "#0b7a8a",
        "#2e7d32",
        "#6a3fb5",
        "#c2185b",
        "#c4441c",
        "#8a5a00",
        "#4a5a6a",
    ];

    public static bool IsValid(string? color)
    {
        return color is not null && ColorRegex().IsMatch(color);
    }

    public static string Normalize(string color)
    {
        return color.ToLowerInvariant();
    }

    /// <summary>
    /// The palette color used by the fewest profiles; on a tie the first one in palette order.
    /// </summary>
    public static string PickDefault(IEnumerable<string> usedColors)
    {
        var counts = Palette.ToDictionary(color => color, _ => 0);
        foreach (string color in usedColors)
        {
            string normalized = Normalize(color);
            if (counts.TryGetValue(normalized, out int count))
            {
                counts[normalized] = count + 1;
            }
        }

        return Palette.MinBy(color => counts[color])!;
    }

    [GeneratedRegex(@"^#[0-9a-fA-F]{6}\z")]
    private static partial Regex ColorRegex();
}
