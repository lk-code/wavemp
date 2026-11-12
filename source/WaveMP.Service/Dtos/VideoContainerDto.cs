using System.Text.Json.Serialization;
using WaveMP.Service.Converters;

namespace WaveMP.Service.Dtos;

/// <summary>
/// A video container as clients check it, for example with canPlayType of a video element.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<VideoContainerDto>))]
public enum VideoContainerDto
{
    /// <summary>
    /// MP4 (<c>video/mp4</c>).
    /// </summary>
    Mp4,

    /// <summary>
    /// QuickTime (<c>video/quicktime</c>).
    /// </summary>
    Mov,

    /// <summary>
    /// WebM (<c>video/webm</c>).
    /// </summary>
    Webm,

    /// <summary>
    /// Matroska (<c>video/x-matroska</c>).
    /// </summary>
    Matroska,
}
