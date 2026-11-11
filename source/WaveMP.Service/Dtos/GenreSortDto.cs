using System.Text.Json.Serialization;
using WaveMP.Service.Converters;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Sort key of the genre list; ties are sorted by name.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<GenreSortDto>))]
public enum GenreSortDto
{
    Name,
    TrackCount,
}
