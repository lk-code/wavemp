namespace WaveMP.Core.Transcoding.Accelerators;

/// <summary>
/// State of the hardware acceleration.
/// </summary>
public enum HardwareAccelerationState
{
    /// <summary>
    /// The detection runs (after the start or a change of the settings).
    /// </summary>
    Detecting,

    /// <summary>
    /// The hardware works; videos are encoded with it.
    /// </summary>
    Active,

    /// <summary>
    /// No usable hardware was found; videos are encoded in software.
    /// </summary>
    Unavailable,

    /// <summary>
    /// The hardware is turned off in the settings.
    /// </summary>
    Disabled,

    /// <summary>
    /// The hardware passed the test, but a transcode failed with it; videos are encoded in software until the next
    /// detection.
    /// </summary>
    Failed,
}
