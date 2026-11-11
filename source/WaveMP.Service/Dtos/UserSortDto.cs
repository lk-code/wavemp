using System.Text.Json.Serialization;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Sort key of the user list; ties are sorted by id.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<UserSortDto>))]
public enum UserSortDto
{
    Name,
    CreatedAt,
}
