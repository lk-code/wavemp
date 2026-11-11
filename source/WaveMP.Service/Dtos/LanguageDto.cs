using System.Text.Json.Serialization;
using WaveMP.Service.Converters;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Language of the user interface.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<LanguageDto>))]
public enum LanguageDto
{
    [JsonStringEnumMemberName("en")]
    En,

    [JsonStringEnumMemberName("de")]
    De,
}
