using System.Text.Json.Serialization;
using WaveMP.Service.Converters;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Whether and how a client evens out the volume of tracks with their ReplayGain tags.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<ReplayGainModeDto>))]
public enum ReplayGainModeDto
{
    /// <summary>
    /// The volume stays as it is.
    /// </summary>
    Off,

    /// <summary>
    /// Every track plays with its own gain; the gain of the album if it has none.
    /// </summary>
    Track,

    /// <summary>
    /// Tracks play with the gain of their album, so the differences within an album stay; the gain of the track if
    /// there is none.
    /// </summary>
    Album,
}
