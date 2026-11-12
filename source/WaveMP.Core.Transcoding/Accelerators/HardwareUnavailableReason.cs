namespace WaveMP.Core.Transcoding.Accelerators;

/// <summary>
/// Why the hardware acceleration is not used.
/// </summary>
public enum HardwareUnavailableReason
{
    /// <summary>
    /// ffmpeg cannot be started; there is no transcoding at all.
    /// </summary>
    FfmpegMissing,

    /// <summary>
    /// There is no render device (<c>/dev/dri</c> is not passed to the container).
    /// </summary>
    NoDevice,

    /// <summary>
    /// The test encodes with Quick Sync and VA-API failed (driver, permissions or a graphics unit without H.264
    /// encoder).
    /// </summary>
    TestFailed,

    /// <summary>
    /// A transcode failed with the hardware.
    /// </summary>
    EncodeFailed,
}
