using System.Text.Json.Serialization;
using WaveMP.Service.Converters;

namespace WaveMP.Service.Dtos;

/// <summary>
/// The audio codec of a video as clients check it.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<AudioCodecDto>))]
public enum AudioCodecDto
{
    /// <summary>
    /// AAC.
    /// </summary>
    Aac,

    /// <summary>
    /// MP3.
    /// </summary>
    Mp3,

    /// <summary>
    /// Opus.
    /// </summary>
    Opus,

    /// <summary>
    /// Vorbis.
    /// </summary>
    Vorbis,

    /// <summary>
    /// FLAC.
    /// </summary>
    Flac,

    /// <summary>
    /// Dolby Digital (AC-3).
    /// </summary>
    Ac3,

    /// <summary>
    /// Dolby Digital Plus (E-AC-3).
    /// </summary>
    Eac3,
}
