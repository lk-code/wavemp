namespace WaveMP.Core.Caching.Covers;

/// <summary>
/// Outcome of preparing the scaled pictures of a list of covers.
/// </summary>
public enum CoverPrepareOutcome
{
    /// <summary>
    /// The cache already has all sizes of the cover shown.
    /// </summary>
    Existing,

    /// <summary>
    /// The missing sizes were created.
    /// </summary>
    Created,

    /// <summary>
    /// None of the covers can be read.
    /// </summary>
    NoReadableSource,

    /// <summary>
    /// The cover cannot be converted or stored, now or within the last minutes.
    /// </summary>
    Failed,

    /// <summary>
    /// ffmpeg cannot be started.
    /// </summary>
    EncoderUnavailable,

    /// <summary>
    /// The cache folder cannot be written.
    /// </summary>
    StoreUnavailable,

    /// <summary>
    /// The cache is nearly full; the preparation never removes files.
    /// </summary>
    LimitReached,
}
