using System.Text.Json.Serialization;
using WaveMP.Service.Converters;

namespace WaveMP.Service.Dtos;

/// <summary>
/// What an HLS stream of a track carries.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<HlsMediaDto>))]
public enum HlsMediaDto
{
    /// <summary>
    /// Only the sound: of the audio file or, for tracks with a video only, of the video.
    /// </summary>
    Audio,

    /// <summary>
    /// The video with its sound.
    /// </summary>
    Video,
}
