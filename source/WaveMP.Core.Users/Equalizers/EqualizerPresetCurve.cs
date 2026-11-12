using WaveMP.Data.Entities;

namespace WaveMP.Core.Users.Equalizers;

/// <summary>
/// The curve of a preset of the equalizer.
/// </summary>
/// <param name="Preset">The preset.</param>
/// <param name="GainsDb">Gain of every band in dB, from the lowest to the highest frequency.</param>
public sealed record EqualizerPresetCurve(EqualizerPreset Preset, IReadOnlyList<double> GainsDb);
