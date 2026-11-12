using WaveMP.Data.Entities;

namespace WaveMP.Core.Users.Equalizers;

/// <summary>
/// The bands of the equalizer: ten octave bands from 31 Hz to 16 kHz. The lowest band is a low shelf, the highest a
/// high shelf, the others are peaking filters one octave wide.
/// </summary>
public static class EqualizerBands
{
    /// <summary>
    /// Number of bands.
    /// </summary>
    public const int Count = UserSettings.EqualizerBandCount;

    /// <summary>
    /// Lowest gain of a band in dB.
    /// </summary>
    public const int MinGainDb = -12;

    /// <summary>
    /// Highest gain of a band in dB.
    /// </summary>
    public const int MaxGainDb = 12;

    /// <summary>
    /// Quality factor of a peaking filter about one octave wide.
    /// </summary>
    public const double PeakingQ = 1.41;

    /// <summary>
    /// Quality factor of a shelf filter without overshoot.
    /// </summary>
    public const double ShelfQ = 0.71;

    /// <summary>
    /// The bands from the lowest to the highest frequency.
    /// </summary>
    public static IReadOnlyList<EqualizerBand> All { get; } =
    [
        new(31, EqualizerFilterType.LowShelf, ShelfQ),
        new(62, EqualizerFilterType.Peaking, PeakingQ),
        new(125, EqualizerFilterType.Peaking, PeakingQ),
        new(250, EqualizerFilterType.Peaking, PeakingQ),
        new(500, EqualizerFilterType.Peaking, PeakingQ),
        new(1000, EqualizerFilterType.Peaking, PeakingQ),
        new(2000, EqualizerFilterType.Peaking, PeakingQ),
        new(4000, EqualizerFilterType.Peaking, PeakingQ),
        new(8000, EqualizerFilterType.Peaking, PeakingQ),
        new(16000, EqualizerFilterType.HighShelf, ShelfQ),
    ];
}
