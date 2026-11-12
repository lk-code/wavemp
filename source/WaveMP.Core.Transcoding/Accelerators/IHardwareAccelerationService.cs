namespace WaveMP.Core.Transcoding.Accelerators;

/// <summary>
/// Detects Intel Quick Sync (QSV, then VA-API) on the render device and tells which encoder new transcodes use.
/// The detection runs in the background after the start and again when the setting changes; it never blocks the
/// start.
/// </summary>
public interface IHardwareAccelerationService
{
    /// <summary>
    /// The result of the last detection, or <see cref="HardwareAccelerationStatus.Detecting"/>.
    /// </summary>
    HardwareAccelerationStatus Current { get; }

    /// <summary>
    /// Runs the detection now (one at a time) and returns its result.
    /// </summary>
    Task<HardwareAccelerationStatus> DetectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// A transcode failed with <paramref name="accelerator"/> before its first segment: new transcodes use software
    /// until the next detection.
    /// </summary>
    void ReportEncodeFailure(HardwareAccelerator accelerator);
}
