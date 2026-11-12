using System.Text.Json.Serialization;
using WaveMP.Service.Converters;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Curve of the equalizer. The curves of the presets come from <c>GET /api/v1/equalizer</c>.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<EqualizerPresetDto>))]
public enum EqualizerPresetDto
{
    /// <summary>
    /// All bands at 0 dB.
    /// </summary>
    Flat,

    /// <summary>
    /// Stronger bass and treble for rock music.
    /// </summary>
    Rock,

    /// <summary>
    /// Emphasized mids for pop music.
    /// </summary>
    Pop,

    /// <summary>
    /// Warm bass and clear treble for jazz.
    /// </summary>
    Jazz,

    /// <summary>
    /// Slightly raised bass and treble for classical music.
    /// </summary>
    Classical,

    /// <summary>
    /// Strong bass and bright treble for dance music.
    /// </summary>
    Dance,

    /// <summary>
    /// Raised bass.
    /// </summary>
    Bass,

    /// <summary>
    /// Raised treble.
    /// </summary>
    Treble,

    /// <summary>
    /// Emphasized range of the voice.
    /// </summary>
    Vocal,

    /// <summary>
    /// The own curve of the profile (<c>customGainsDb</c>).
    /// </summary>
    Custom,
}
