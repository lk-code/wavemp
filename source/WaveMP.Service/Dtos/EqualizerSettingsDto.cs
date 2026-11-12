namespace WaveMP.Service.Dtos;

/// <summary>
/// Equalizer of the current profile.
/// </summary>
public sealed class EqualizerSettingsDto
{
    /// <summary>
    /// Whether the players shape the sound with the equalizer; false by default.
    /// </summary>
    public required bool Enabled { get; init; }

    /// <summary>
    /// The selected curve; Flat by default.
    /// </summary>
    public required EqualizerPresetDto Preset { get; init; }

    /// <summary>
    /// Gain in dB before the bands, from -12 to 12; 0 by default.
    /// </summary>
    public required double PreampDb { get; init; }

    /// <summary>
    /// The curve that applies, one gain in dB per band from the lowest to the highest frequency: the curve of the
    /// preset, or <c>customGainsDb</c> for Custom. Read only.
    /// </summary>
    public required IReadOnlyList<double> GainsDb { get; init; }

    /// <summary>
    /// The own curve of the profile, one gain in dB per band from -12 to 12. It stays when the profile selects a
    /// preset.
    /// </summary>
    public required IReadOnlyList<double> CustomGainsDb { get; init; }
}
