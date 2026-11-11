namespace WaveMP.Core.Settings.Validations;

/// <summary>
/// A rejected value of the library settings.
/// </summary>
public sealed record SettingsValidationError(LibrarySettingsField Field, SettingsErrorReason Reason);
