using System.Text.Json.Serialization;
using WaveMP.Service.Converters;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Filter of a band of the equalizer, like the filter types of common audio APIs (e.g. a biquad filter).
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<EqualizerFilterTypeDto>))]
public enum EqualizerFilterTypeDto
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
