using WaveMP.Core.Transcoding.Accelerators;

namespace WaveMP.Core.Transcoding.Services;

/// <summary>
/// State of the transcoding for the admin area.
/// </summary>
/// <param name="Hardware">Result of the hardware detection.</param>
/// <param name="CacheAvailable">Whether the cache, which holds the segments, can be written.</param>
/// <param name="RunningJobs">Number of running transcodes.</param>
/// <param name="MaxParallelJobs">Limit of parallel transcodes from the settings.</param>
public sealed record TranscodingStatus(
    HardwareAccelerationStatus Hardware,
    bool CacheAvailable,
    int RunningJobs,
    int MaxParallelJobs)
{
    /// <summary>
    /// Whether anything can be transcoded.
    /// </summary>
    public bool Available => Hardware.FfmpegAvailable && CacheAvailable;
}
