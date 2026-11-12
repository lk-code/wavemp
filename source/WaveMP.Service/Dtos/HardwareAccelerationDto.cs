namespace WaveMP.Service.Dtos;

/// <summary>
/// Result of the detection of the graphics hardware.
/// </summary>
public sealed class HardwareAccelerationDto
{
    /// <summary>
    /// State of the hardware acceleration.
    /// </summary>
    public required HardwareAccelerationStateDto State { get; init; }

    /// <summary>
    /// The encoder that passed the test; none without.
    /// </summary>
    public required HardwareAcceleratorDto Accelerator { get; init; }

    /// <summary>
    /// The render device that was tested, if any.
    /// </summary>
    public string? Device { get; init; }

    /// <summary>
    /// Why the hardware is not used; null while detecting, active or disabled.
    /// </summary>
    public HardwareUnavailableReasonDto? Reason { get; init; }

    /// <summary>
    /// When the detection finished; null while detecting.
    /// </summary>
    public DateTimeOffset? DetectedAt { get; init; }
}
