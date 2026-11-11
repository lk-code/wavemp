namespace WaveMP.Service.Dtos;

/// <summary>
/// Settings of the media library.
/// </summary>
public sealed class LibrarySettingsDto
{
    /// <summary>
    /// Minutes between two periodic scans (15 to 1440). The periodic scan always runs.
    /// </summary>
    public required int ScanIntervalMinutes { get; init; }

    /// <summary>
    /// Watches the media folder for changes and scans changed folders shortly after.
    /// </summary>
    public required bool FileWatcherEnabled { get; init; }

    /// <summary>
    /// Extensions of audio files, lower case without dot.
    /// </summary>
    public required IReadOnlyList<string> AudioExtensions { get; init; }

    /// <summary>
    /// Extensions of video files, lower case without dot.
    /// </summary>
    public required IReadOnlyList<string> VideoExtensions { get; init; }

    /// <summary>
    /// Extensions of pictures (covers), lower case without dot.
    /// </summary>
    public required IReadOnlyList<string> ImageExtensions { get; init; }

    /// <summary>
    /// Base names of pictures that are album covers, in the order of their priority.
    /// </summary>
    public required IReadOnlyList<string> AlbumCoverNames { get; init; }
}
