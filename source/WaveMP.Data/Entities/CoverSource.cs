namespace WaveMP.Data.Entities;

/// <summary>
/// Where a cover comes from. Stored as its name.
/// </summary>
public enum CoverSource
{
    /// <summary>
    /// A picture file next to the media files.
    /// </summary>
    File,

    /// <summary>
    /// A picture embedded in an audio or video file.
    /// </summary>
    Embedded,
}
