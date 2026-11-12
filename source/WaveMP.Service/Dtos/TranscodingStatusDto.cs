namespace WaveMP.Service.Dtos;

/// <summary>
/// State of the transcoding.
/// </summary>
public sealed class TranscodingStatusDto
{
    /// <summary>
    /// Whether anything can be transcoded (ffmpeg can be started and the cache can be written).
    /// </summary>
    public required bool Available { get; init; }

    /// <summary>
    /// Whether ffmpeg can be started.
    /// </summary>
    public required bool FfmpegAvailable { get; init; }

    /// <summary>
    /// Whether the cache, which holds the segments, can be written.
    /// </summary>
    public required bool CacheAvailable { get; init; }

    /// <summary>
    /// Result of the detection of the graphics hardware.
    /// </summary>
    public required HardwareAccelerationDto Hardware { get; init; }

    /// <summary>
    /// Number of running transcodes.
    /// </summary>
    public required int RunningJobs { get; init; }

    /// <summary>
    /// Limit of parallel transcodes from the settings.
    /// </summary>
    public required int MaxParallelJobs { get; init; }
}
