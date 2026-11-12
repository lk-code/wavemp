using System.Text.Json.Serialization;
using WaveMP.Service.Converters;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Where the cover of a playlist comes from.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<PlaylistCoverKindDto>))]
public enum PlaylistCoverKindDto
{
    /// <summary>
    /// A picture uploaded for the playlist, delivered by its cover endpoint.
    /// </summary>
    Custom,

    /// <summary>
    /// The covers of some tracks of the playlist.
    /// </summary>
    Tracks,

    /// <summary>
    /// No cover; the playlist is empty or none of its tracks has a cover.
    /// </summary>
    None,
}
