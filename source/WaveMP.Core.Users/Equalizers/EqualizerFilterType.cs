namespace WaveMP.Core.Users.Equalizers;

/// <summary>
/// Filter of a band of the equalizer.
/// </summary>
public enum EqualizerFilterType
{
    /// <summary>
    /// Raises or lowers everything below the frequency of the band.
    /// </summary>
    LowShelf,

    /// <summary>
    /// Raises or lowers the frequencies around the frequency of the band.
    /// </summary>
    Peaking,

    /// <summary>
    /// Raises or lowers everything above the frequency of the band.
    /// </summary>
    HighShelf,
}
