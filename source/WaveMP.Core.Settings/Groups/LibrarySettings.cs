namespace WaveMP.Core.Settings.Groups;

/// <summary>
/// Settings of the media library: how often the media folder is scanned, whether changes are watched and which files
/// belong to the library. Stored as the settings group <see cref="Key"/>; values missing in the stored JSON keep the
/// defaults below. Extensions are lower case without a dot.
/// </summary>
public sealed class LibrarySettings
{
    /// <summary>
    /// Key of the settings group.
    /// </summary>
    public const string Key = "library";

    /// <summary>
    /// Minutes between two periodic scans. The periodic scan always runs, also with the file watcher, because file
    /// system events are unreliable on network shares.
    /// </summary>
    public int ScanIntervalMinutes { get; init; } = 360;

    /// <summary>
    /// Watches the media folder for changes and scans changed folders shortly after.
    /// </summary>
    public bool FileWatcherEnabled { get; init; } = true;

    public IReadOnlyList<string> AudioExtensions { get; init; } =
    [
        "mp3", "flac", "m4a", "aac", "ogg", "oga", "opus", "wav", "wma", "aif", "aiff", "ape", "wv", "mka", "mpc", "dsf",
    ];

    public IReadOnlyList<string> VideoExtensions { get; init; } =
        ["mp4", "m4v", "mkv", "webm", "mov", "avi", "wmv", "mpg", "mpeg", "ts"];

    public IReadOnlyList<string> ImageExtensions { get; init; } = ["jpg", "jpeg", "png", "webp", "gif", "bmp"];

    /// <summary>
    /// Base names of pictures that are the cover of the album in their folder, in the order of their priority.
    /// Lower case; compared without regard to case.
    /// </summary>
    public IReadOnlyList<string> AlbumCoverNames { get; init; } = ["cover", "folder", "front", "album", "albumart"];
}
