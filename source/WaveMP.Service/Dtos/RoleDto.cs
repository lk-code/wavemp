using System.Text.Json.Serialization;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Role of a profile.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<RoleDto>))]
public enum RoleDto
{
    Admin,
    User,
}
