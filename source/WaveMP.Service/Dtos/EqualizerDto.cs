namespace WaveMP.Service.Dtos;

/// <summary>
/// The equalizer of the players: its bands, the limits of the gains and the curves of the presets. The same for every
/// profile; clients apply the curves with it instead of keeping own tables.
/// </summary>
public sealed class EqualizerDto
{
    /// <summary>
    /// Lowest gain of a band and of the preamp in dB.
    /// </summary>
    public required int MinGainDb { get; init; }

    /// <summary>
    /// Highest gain of a band and of the preamp in dB.
    /// </summary>
    public required int MaxGainDb { get; init; }

    /// <summary>
    /// The bands from the lowest to the highest frequency; the gains of a curve follow this order.
    /// </summary>
    public required IReadOnlyList<EqualizerBandDto> Bands { get; init; }

    /// <summary>
    /// The curves of all presets except Custom.
    /// </summary>
    public required IReadOnlyList<EqualizerPresetCurveDto> Presets { get; init; }
}
