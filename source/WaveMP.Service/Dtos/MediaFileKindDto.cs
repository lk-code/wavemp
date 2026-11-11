using System.Text.Json.Serialization;
using WaveMP.Service.Converters;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Kind of a media file.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<MediaFileKindDto>))]
public enum MediaFileKindDto
{
    Audio,
    Video,
    Image,
}
