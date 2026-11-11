namespace WaveMP.Core.Caching.Images;

/// <summary>
/// Outcome of converting a cover.
/// </summary>
public enum CoverEncodeStatus
{
    /// <summary>
    /// All pictures were written.
    /// </summary>
    Encoded,

    /// <summary>
    /// The converter cannot be started (ffmpeg is missing).
    /// </summary>
    Unavailable,

    /// <summary>
    /// The picture cannot be converted, e.g. because it is damaged or the conversion took too long. Nothing was
    /// written.
    /// </summary>
    Failed,
}
