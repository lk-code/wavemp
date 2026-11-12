namespace WaveMP.Core.Users.Equalizers;

/// <summary>
/// Checks and rounds gains of the equalizer.
/// </summary>
public static class EqualizerGains
{
    /// <summary>
    /// Whether <paramref name="gain"/> is a finite gain from <see cref="EqualizerBands.MinGainDb"/> to
    /// <see cref="EqualizerBands.MaxGainDb"/>.
    /// </summary>
    public static bool IsValid(double gain)
    {
        return double.IsFinite(gain) && gain is >= EqualizerBands.MinGainDb and <= EqualizerBands.MaxGainDb;
    }

    /// <summary>
    /// Whether <paramref name="gains"/> has exactly one valid gain per band.
    /// </summary>
    public static bool IsValid(IReadOnlyList<double> gains)
    {
        return gains.Count == EqualizerBands.Count && gains.All(IsValid);
    }

    /// <summary>
    /// The gain rounded to 0.1 dB, the precision the settings keep.
    /// </summary>
    public static double Normalize(double gain)
    {
        double rounded = Math.Round(gain, 1, MidpointRounding.AwayFromZero);

        // Rounding e.g. -0.04 gives negative zero, which JSON would show as "-0".
        return rounded == 0 ? 0 : rounded;
    }

    /// <summary>
    /// The gains rounded to 0.1 dB.
    /// </summary>
    public static double[] Normalize(IReadOnlyList<double> gains)
    {
        return [.. gains.Select(Normalize)];
    }
}
