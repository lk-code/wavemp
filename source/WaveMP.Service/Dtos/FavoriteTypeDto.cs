using System.Text.Json.Serialization;
using WaveMP.Service.Converters;

namespace WaveMP.Service.Dtos;

/// <summary>
/// What a favorite marks.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<FavoriteTypeDto>))]
public enum FavoriteTypeDto
{
    Track,
    Album,
    Artist,
}
