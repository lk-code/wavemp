using System.Text.RegularExpressions;
using WaveMP.Core.Settings.Groups;

namespace WaveMP.Core.Settings.Validations;

/// <summary>
/// Normalizes and checks the library settings before they are saved.
/// </summary>
public static partial class LibrarySettingsValidator
{
    public const int MinScanIntervalMinutes = 15;
    public const int MaxScanIntervalMinutes = 1440;
    public const int MaxEntries = 50;
    public const int MaxCoverNameLength = 50;

    /// <summary>
    /// Trims the list entries, removes a leading dot of extensions, writes them in lower case and removes empty and
    /// duplicate entries; the order of the other entries stays.
    /// </summary>
    public static LibrarySettings Normalize(LibrarySettings settings)
    {
        return new LibrarySettings
        {
            ScanIntervalMinutes = settings.ScanIntervalMinutes,
            FileWatcherEnabled = settings.FileWatcherEnabled,
            AudioExtensions = NormalizeList(settings.AudioExtensions, trimDot: true),
            VideoExtensions = NormalizeList(settings.VideoExtensions, trimDot: true),
            ImageExtensions = NormalizeList(settings.ImageExtensions, trimDot: true),
            AlbumCoverNames = NormalizeList(settings.AlbumCoverNames, trimDot: false),
            CoverPriority = settings.CoverPriority,
        };
    }

    /// <summary>
    /// Checks normalized settings and returns all errors; an empty list means they are valid.
    /// </summary>
    public static IReadOnlyList<SettingsValidationError> Validate(LibrarySettings settings)
    {
        List<SettingsValidationError> errors = [];

        if (settings.ScanIntervalMinutes is < MinScanIntervalMinutes or > MaxScanIntervalMinutes)
        {
            errors.Add(new(
                LibrarySettingsField.ScanIntervalMinutes,
                SettingsErrorReason.OutOfRange,
                MinScanIntervalMinutes,
                MaxScanIntervalMinutes));
        }

        (LibrarySettingsField Field, IReadOnlyList<string> Values)[] extensionLists =
        [
            (LibrarySettingsField.AudioExtensions, settings.AudioExtensions),
            (LibrarySettingsField.VideoExtensions, settings.VideoExtensions),
            (LibrarySettingsField.ImageExtensions, settings.ImageExtensions),
        ];

        HashSet<string> earlierExtensions = new(StringComparer.Ordinal);
        foreach ((LibrarySettingsField field, IReadOnlyList<string> values) in extensionLists)
        {
            if (values.Count > MaxEntries)
            {
                errors.Add(new(field, SettingsErrorReason.TooMany, Maximum: MaxEntries));
            }
            else if (!values.All(value => ExtensionPattern().IsMatch(value)))
            {
                errors.Add(new(field, SettingsErrorReason.Invalid));
            }
            else if (values.Any(earlierExtensions.Contains))
            {
                errors.Add(new(field, SettingsErrorReason.Overlap));
            }

            earlierExtensions.UnionWith(values);
        }

        if (settings.AudioExtensions.Count == 0 && settings.VideoExtensions.Count == 0)
        {
            errors.Add(new(LibrarySettingsField.AudioExtensions, SettingsErrorReason.Empty));
            errors.Add(new(LibrarySettingsField.VideoExtensions, SettingsErrorReason.Empty));
        }

        if (settings.AlbumCoverNames.Count > MaxEntries)
        {
            errors.Add(new(LibrarySettingsField.AlbumCoverNames, SettingsErrorReason.TooMany, Maximum: MaxEntries));
        }
        else if (!settings.AlbumCoverNames.All(IsValidCoverName))
        {
            errors.Add(new(LibrarySettingsField.AlbumCoverNames, SettingsErrorReason.Invalid));
        }

        // The stored JSON may hold a number instead of the name.
        if (!Enum.IsDefined(settings.CoverPriority))
        {
            errors.Add(new(LibrarySettingsField.CoverPriority, SettingsErrorReason.Invalid));
        }

        return errors;
    }

    private static bool IsValidCoverName(string name)
    {
        return name.Length <= MaxCoverNameLength
            && !name.StartsWith('.')
            && name.IndexOfAny(['/', '\\']) < 0
            && !name.Any(char.IsControl);
    }

    private static string[] NormalizeList(IReadOnlyList<string>? values, bool trimDot)
    {
        return [.. (values ?? [])
            .Select(value => (value ?? string.Empty).Trim())
            .Select(value => trimDot ? value.TrimStart('.').Trim() : value)
            .Select(value => value.ToLowerInvariant())
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.Ordinal)];
    }

    [GeneratedRegex("^[a-z0-9]{1,10}$", RegexOptions.CultureInvariant)]
    private static partial Regex ExtensionPattern();
}
