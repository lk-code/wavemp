using System.Text.Json.Serialization;
using WaveMP.Service.Converters;

namespace WaveMP.Service.Dtos;

/// <summary>
/// How the stream of <c>streamUrl</c> is delivered.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<StreamProtocolDto>))]
public enum StreamProtocolDto
{
    /// <summary>
    /// The file as it is, with HTTP range requests.
    /// </summary>
    File,

    /// <summary>
    /// An HLS master playlist (<c>application/vnd.apple.mpegurl</c>) of MPEG-TS segments, transcoded with ffmpeg
    /// while they are requested. Clients play it natively or with an HLS library.
    /// </summary>
    Hls,
}
