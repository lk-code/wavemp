namespace WaveMP.Core.Settings.Validations;

/// <summary>
/// A rejected value of the library settings, with the limits that apply (<see cref="SettingsErrorReason.OutOfRange"/>:
/// both, <see cref="SettingsErrorReason.TooMany"/>: the maximum).
/// </summary>
public sealed record SettingsValidationError(
    LibrarySettingsField Field,
    SettingsErrorReason Reason,
    int? Minimum = null,
    int? Maximum = null);
