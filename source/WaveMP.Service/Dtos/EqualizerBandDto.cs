namespace WaveMP.Service.Dtos;

/// <summary>
/// A band of the equalizer.
/// </summary>
public sealed class EqualizerBandDto
{
    /// <summary>
    /// Center frequency of a peaking filter, corner frequency of a shelf filter, in Hz.
    /// </summary>
    public required int FrequencyHz { get; init; }

    /// <summary>
    /// Filter of the band.
    /// </summary>
    public required EqualizerFilterTypeDto FilterType { get; init; }

    /// <summary>
    /// Quality factor of the filter: the higher, the narrower the band.
    /// </summary>
    public required double Q { get; init; }
}
