using System.Text.Json.Serialization;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Language of the user interface.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<LanguageDto>))]
public enum LanguageDto
{
    [JsonStringEnumMemberName("en")]
    En,

    [JsonStringEnumMemberName("de")]
    De,
}
