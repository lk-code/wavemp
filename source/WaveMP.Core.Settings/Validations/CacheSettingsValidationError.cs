namespace WaveMP.Core.Settings.Validations;

/// <summary>
/// A rejected value of the cache settings.
/// </summary>
public sealed record CacheSettingsValidationError(CacheSettingsField Field, SettingsErrorReason Reason);
