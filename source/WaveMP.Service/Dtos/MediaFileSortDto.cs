using System.Text.Json.Serialization;
using WaveMP.Service.Converters;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Sort key of the files of a track. Kind lists audio, video, then pictures, the primary file first.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<MediaFileSortDto>))]
public enum MediaFileSortDto
{
    Kind,
    Name,
    Size,
}
