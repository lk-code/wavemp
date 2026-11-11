using System.Text.Json.Serialization;
using WaveMP.Service.Converters;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Sort key of the track list; ties are sorted by id. AlbumArtist, Album, Artist, Year, Genre and FirstSeenAt keep the
/// tracks of an album together in the order of disc and track number. Relevance needs q, otherwise the default applies.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<TrackSortDto>))]
public enum TrackSortDto
{
    AlbumArtist,
    Album,
    Title,
    Artist,
    Duration,
    Year,
    Genre,
    FirstSeenAt,
    Relevance,
}
