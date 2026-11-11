namespace WaveMP.Core.Settings.Validations;

/// <summary>
/// A rejected value of the cache settings, with the limits that apply (<see cref="SettingsErrorReason.OutOfRange"/>:
/// both).
/// </summary>
public sealed record CacheSettingsValidationError(
    CacheSettingsField Field,
    SettingsErrorReason Reason,
    int? Minimum = null,
    int? Maximum = null);
