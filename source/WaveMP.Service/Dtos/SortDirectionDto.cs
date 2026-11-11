using System.Text.Json.Serialization;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Sort direction.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<SortDirectionDto>))]
public enum SortDirectionDto
{
    Asc,
    Desc,
}
