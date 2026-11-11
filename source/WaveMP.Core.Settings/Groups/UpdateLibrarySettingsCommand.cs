namespace WaveMP.Core.Settings.Groups;

/// <summary>
/// Changes the library settings. Every <c>null</c> value keeps the current value.
/// </summary>
public sealed record UpdateLibrarySettingsCommand(
    int? ScanIntervalMinutes = null,
    bool? FileWatcherEnabled = null,
    IReadOnlyList<string>? AudioExtensions = null,
    IReadOnlyList<string>? VideoExtensions = null,
    IReadOnlyList<string>? ImageExtensions = null,
    IReadOnlyList<string>? AlbumCoverNames = null);
