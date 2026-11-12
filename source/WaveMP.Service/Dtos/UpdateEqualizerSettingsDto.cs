using System.ComponentModel.DataAnnotations;
using WaveMP.Core.Users.Equalizers;
using WaveMP.Core.Users.Settings;
using WaveMP.Service.Validators;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Changes the equalizer of the current profile. Missing or null values stay unchanged.
/// </summary>
public sealed class UpdateEqualizerSettingsDto
{
    /// <summary>
    /// Whether the players shape the sound with the equalizer.
    /// </summary>
    public bool? Enabled { get; init; }

    /// <summary>
    /// The selected curve.
    /// </summary>
    public EqualizerPresetDto? Preset { get; init; }

    /// <summary>
    /// Gain in dB before the bands, from -12 to 12; rounded to 0.1 dB.
    /// </summary>
    [Range((double)PlaybackSettingsLimits.PreampMinDb, (double)PlaybackSettingsLimits.PreampMaxDb)]
    public double? PreampDb { get; init; }

    /// <summary>
    /// The own curve of the profile: exactly one gain per band (10), each from -12 to 12 dB, rounded to 0.1 dB. It
    /// does not select the preset Custom by itself; clients send the preset Custom with it to apply the curve.
    /// </summary>
    [Length(EqualizerBands.Count, EqualizerBands.Count)]
    [EachInRange(EqualizerBands.MinGainDb, EqualizerBands.MaxGainDb)]
    public IReadOnlyList<double>? CustomGainsDb { get; init; }
}
