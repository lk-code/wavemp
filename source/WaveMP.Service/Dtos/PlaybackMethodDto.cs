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
    /// The client plays the file as it is from <c>streamUrl</c> (protocol <c>File</c>).
    /// </summary>
    DirectPlay,

    /// <summary>
    /// The client cannot play the file; the server converts it with ffmpeg into the HLS stream of <c>streamUrl</c>
    /// (protocol <c>Hls</c>).
    /// </summary>
    Transcode,
}
