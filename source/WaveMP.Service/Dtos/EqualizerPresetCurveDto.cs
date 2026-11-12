namespace WaveMP.Service.Dtos;

/// <summary>
/// The curve of a preset of the equalizer.
/// </summary>
public sealed class EqualizerPresetCurveDto
{
    /// <summary>
    /// The preset.
    /// </summary>
    public required EqualizerPresetDto Preset { get; init; }

    /// <summary>
    /// Gain of every band in dB, from the lowest to the highest frequency.
    /// </summary>
    public required IReadOnlyList<double> GainsDb { get; init; }
}
