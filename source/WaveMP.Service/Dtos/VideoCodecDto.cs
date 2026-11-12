using System.Text.Json.Serialization;
using WaveMP.Service.Converters;

namespace WaveMP.Service.Dtos;

/// <summary>
/// A video codec as clients check it.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<VideoCodecDto>))]
public enum VideoCodecDto
{
    /// <summary>
    /// H.264 / AVC; played directly only as 8 bit 4:2:0.
    /// </summary>
    H264,

    /// <summary>
    /// H.265 / HEVC.
    /// </summary>
    Hevc,

    /// <summary>
    /// VP8.
    /// </summary>
    Vp8,

    /// <summary>
    /// VP9.
    /// </summary>
    Vp9,

    /// <summary>
    /// AV1.
    /// </summary>
    Av1,
}
