using WaveMP.Core.Settings.Groups;

namespace WaveMP.Core.Settings.Validations;

/// <summary>
/// Checks the cache settings before they are saved.
/// </summary>
public static class CacheSettingsValidator
{
    public const int MinMaxSizeMegabytes = 100;
    public const int MaxMaxSizeMegabytes = 1_000_000;

    /// <summary>
    /// All errors of the settings; an empty list means they are valid.
    /// </summary>
    public static IReadOnlyList<CacheSettingsValidationError> Validate(CacheSettings settings)
    {
        List<CacheSettingsValidationError> errors = [];
        if (settings.MaxSizeMegabytes is < MinMaxSizeMegabytes or > MaxMaxSizeMegabytes)
        {
            errors.Add(new(CacheSettingsField.MaxSizeMegabytes, SettingsErrorReason.OutOfRange));
        }

        return errors;
    }
}
