using WaveMP.Data.Entities;

namespace WaveMP.Core.Users.Equalizers;

/// <summary>
/// The curves of the presets of the equalizer.
/// </summary>
public static class EqualizerPresets
{
    /// <summary>
    /// The curves of all presets except <see cref="EqualizerPreset.Custom"/>, in the order of the enum.
    /// </summary>
    public static IReadOnlyList<EqualizerPresetCurve> All { get; } =
    [
        new(EqualizerPreset.Flat, [0, 0, 0, 0, 0, 0, 0, 0, 0, 0]),
        new(EqualizerPreset.Rock, [5, 4, 3, 1, -1, -1, 1, 3, 4, 5]),
        new(EqualizerPreset.Pop, [-1, 0, 2, 4, 5, 4, 2, 0, -1, -1]),
        new(EqualizerPreset.Jazz, [4, 3, 1, 2, -2, -2, 0, 1, 3, 4]),
        new(EqualizerPreset.Classical, [5, 4, 3, 2, -1, -1, 0, 2, 3, 4]),
        new(EqualizerPreset.Dance, [6, 5, 2, 0, 0, -2, -2, 0, 4, 5]),
        new(EqualizerPreset.Bass, [8, 7, 5, 3, 1, 0, 0, 0, 0, 0]),
        new(EqualizerPreset.Treble, [0, 0, 0, 0, 0, 1, 3, 5, 7, 8]),
        new(EqualizerPreset.Vocal, [-2, -3, -2, 1, 4, 4, 3, 1, 0, -2]),
    ];

    /// <summary>
    /// The curve that applies for <paramref name="preset"/>: the own curve of the user
    /// (<paramref name="customGains"/>) for <see cref="EqualizerPreset.Custom"/>, otherwise the curve of the preset.
    /// </summary>
    public static IReadOnlyList<double> GainsOf(EqualizerPreset preset, IReadOnlyList<double> customGains)
    {
        if (preset == EqualizerPreset.Custom)
        {
            return customGains;
        }

        return All.FirstOrDefault(curve => curve.Preset == preset)?.GainsDb
            ?? throw new ArgumentOutOfRangeException(nameof(preset), preset, "Unknown equalizer preset.");
    }
}
