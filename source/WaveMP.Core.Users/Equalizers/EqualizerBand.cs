namespace WaveMP.Core.Users.Equalizers;

/// <summary>
/// A band of the equalizer.
/// </summary>
/// <param name="FrequencyHz">Center frequency of a peaking filter, corner frequency of a shelf filter.</param>
/// <param name="FilterType">Filter of the band.</param>
/// <param name="Q">Quality factor of the filter: the higher, the narrower the band.</param>
public sealed record EqualizerBand(int FrequencyHz, EqualizerFilterType FilterType, double Q);
