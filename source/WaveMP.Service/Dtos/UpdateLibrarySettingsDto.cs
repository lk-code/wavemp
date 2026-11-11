using System.ComponentModel.DataAnnotations;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Changes the settings of the media library. Missing or null values stay unchanged.
/// </summary>
public sealed class UpdateLibrarySettingsDto
{
    /// <summary>
    /// Minutes between two periodic scans; the scan is planned again at once.
    /// </summary>
    [Range(15, 1440)]
    public int? ScanIntervalMinutes { get; init; }

    /// <summary>
    /// Switches the file watcher on or off at once.
    /// </summary>
    public bool? FileWatcherEnabled { get; init; }

    /// <summary>
    /// Extensions of audio files (1 to 10 letters or digits; a leading dot is removed); applies to the next scan.
    /// </summary>
    [MaxLength(50)]
    public IReadOnlyList<string>? AudioExtensions { get; init; }

    /// <summary>
    /// Extensions of video files; an extension may only be in one list.
    /// </summary>
    [MaxLength(50)]
    public IReadOnlyList<string>? VideoExtensions { get; init; }

    /// <summary>
    /// Extensions of pictures; may be empty.
    /// </summary>
    [MaxLength(50)]
    public IReadOnlyList<string>? ImageExtensions { get; init; }

    /// <summary>
    /// Base names of album covers (without extension), in the order of their priority.
    /// </summary>
    [MaxLength(50)]
    public IReadOnlyList<string>? AlbumCoverNames { get; init; }
}
