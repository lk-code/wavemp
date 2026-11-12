namespace WaveMP.Core.Transcoding.Accelerators;

/// <summary>
/// What the hardware detection found.
/// </summary>
/// <param name="State">State of the hardware acceleration.</param>
/// <param name="Accelerator">The encoder that passed the test; <see cref="HardwareAccelerator.None"/> without.</param>
/// <param name="Device">The render device that was tested, if any.</param>
/// <param name="Reason">Why the hardware is not used; <c>null</c> while detecting, active or disabled.</param>
/// <param name="FfmpegAvailable">Whether ffmpeg can be started; assumed while detecting.</param>
/// <param name="DetectedAt">When the detection finished; <c>null</c> while detecting.</param>
public sealed record HardwareAccelerationStatus(
    HardwareAccelerationState State,
    HardwareAccelerator Accelerator,
    string? Device,
    HardwareUnavailableReason? Reason,
    bool FfmpegAvailable,
    DateTimeOffset? DetectedAt)
{
    public static HardwareAccelerationStatus Detecting { get; } =
        new(HardwareAccelerationState.Detecting, HardwareAccelerator.None, null, null, true, null);

    /// <summary>
    /// The encoder new transcodes use: the hardware while it is active, otherwise software.
    /// </summary>
    public HardwareAccelerator EncoderOfNewJobs => State == HardwareAccelerationState.Active
        ? Accelerator
        : HardwareAccelerator.None;
}
