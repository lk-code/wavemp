using System.Text.Json.Serialization;
using WaveMP.Service.Converters;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Sort key of the user list; ties are sorted by id.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<UserSortDto>))]
public enum UserSortDto
{
    Name,
    CreatedAt,
}
