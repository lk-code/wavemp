using System.Text.Json.Serialization;
using WaveMP.Service.Converters;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Sort key of the track list; ties are sorted by id. AlbumArtist, Album, Artist, Year, Genre and FirstSeenAt keep the
/// tracks of an album together in the order of disc and track number. Relevance needs q, otherwise the default applies.
/// Rating sorts by the stars of the current profile (unrated tracks count as 0), then by the time of the rating, the
/// latest first.
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
    Rating,
}
