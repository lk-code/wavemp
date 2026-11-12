namespace WaveMP.Core.Streaming.Formats;

/// <summary>
/// A container of videos as clients check it.
/// </summary>
public enum VideoContainer
{
    /// <summary>
    /// MPEG-4 (<c>.mp4</c>, <c>.m4v</c>).
    /// </summary>
    Mp4,

    /// <summary>
    /// QuickTime (<c>.mov</c>).
    /// </summary>
    Mov,

    /// <summary>
    /// WebM (<c>.webm</c>).
    /// </summary>
    Webm,

    /// <summary>
    /// Matroska (<c>.mkv</c>).
    /// </summary>
    Matroska,
}
