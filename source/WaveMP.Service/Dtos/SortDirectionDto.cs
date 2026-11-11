using System.Text.Json.Serialization;
using WaveMP.Service.Converters;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Sort direction.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<SortDirectionDto>))]
public enum SortDirectionDto
{
    Asc,
    Desc,
}
