using System.Text.Json.Serialization;
using WaveMP.Service.Converters;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Sort key of the playlist list; ties are sorted by id.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<PlaylistSortDto>))]
public enum PlaylistSortDto
{
    Name,
    CreatedAt,
    UpdatedAt,
}
