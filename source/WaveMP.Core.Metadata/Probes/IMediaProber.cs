namespace WaveMP.Core.Metadata.Probes;

/// <summary>
/// Reads technical information about media files with ffprobe, run as a separate process.
/// </summary>
public interface IMediaProber
{
    /// <summary>
    /// Whether ffprobe can be started.
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The technical information, or <c>null</c> if ffprobe cannot read the file or takes too long.
    /// </summary>
    /// <exception cref="ProbeUnavailableException">ffprobe cannot be started.</exception>
    Task<MediaProbeResult?> ProbeAsync(string path, CancellationToken cancellationToken = default);
}
