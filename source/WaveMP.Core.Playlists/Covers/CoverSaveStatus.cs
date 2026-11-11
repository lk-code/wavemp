namespace WaveMP.Core.Playlists.Covers;

/// <summary>
/// Outcome of storing an uploaded cover.
/// </summary>
public enum CoverSaveStatus
{
    Saved,

    /// <summary>
    /// The content is no JPEG, PNG or WebP picture.
    /// </summary>
    UnsupportedFormat,

    /// <summary>
    /// The picture is damaged or cannot be converted.
    /// </summary>
    Unreadable,

    /// <summary>
    /// Covers cannot be stored: ffmpeg is missing or the folder cannot be written.
    /// </summary>
    Unavailable,
}
