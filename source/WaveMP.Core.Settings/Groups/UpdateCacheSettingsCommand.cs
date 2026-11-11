namespace WaveMP.Core.Settings.Groups;

/// <summary>
/// Changes the cache settings. Every <c>null</c> value keeps the current value.
/// </summary>
public sealed record UpdateCacheSettingsCommand(int? MaxSizeMegabytes = null);
