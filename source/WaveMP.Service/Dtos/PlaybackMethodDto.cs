using System.Text.Json.Serialization;
using WaveMP.Service.Converters;

namespace WaveMP.Service.Dtos;

/// <summary>
/// How a client plays a track.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<PlaybackMethodDto>))]
public enum PlaybackMethodDto
{
    /// <summary>
    /// The client plays the file as it is from <c>streamUrl</c>.
    /// </summary>
    DirectPlay,

    /// <summary>
    /// The client cannot play the file; the server has to convert it. Transcoding is not available yet, so there is
    /// no <c>streamUrl</c>.
    /// </summary>
    Transcode,
}
