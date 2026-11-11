using System.Text.Json.Serialization;
using WaveMP.Service.Converters;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Role of a profile.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<RoleDto>))]
public enum RoleDto
{
    Admin,
    User,
}
