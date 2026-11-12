namespace WaveMP.Core.Settings.Validations;

/// <summary>
/// A rejected value of the transcoding settings, with the limits that apply (<see cref="SettingsErrorReason.OutOfRange"/>:
/// both; <see cref="SettingsErrorReason.Invalid"/>: none, the allowed values are fixed).
/// </summary>
public sealed record TranscodingSettingsValidationError(
    TranscodingSettingsField Field,
    SettingsErrorReason Reason,
    int? Minimum = null,
    int? Maximum = null);
