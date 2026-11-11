using System.Text.Json.Serialization;
using WaveMP.Service.Converters;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Sort key of the artist list; ties are sorted by name and id. Relevance needs q, otherwise the default applies.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<ArtistSortDto>))]
public enum ArtistSortDto
{
    Name,
    FirstSeenAt,
    Relevance,
}
