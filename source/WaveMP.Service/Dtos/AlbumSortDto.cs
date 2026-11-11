using System.Text.Json.Serialization;
using WaveMP.Service.Converters;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Sort key of the album list; ties are sorted by title and id. Artist is the album artist (compilations and unknown
/// artists last). Relevance needs q, otherwise the default applies.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<AlbumSortDto>))]
public enum AlbumSortDto
{
    Title,
    Artist,
    Year,
    FirstSeenAt,
    Relevance,
}
