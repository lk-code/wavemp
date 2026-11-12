using System.Text.Json.Serialization;
using WaveMP.Service.Converters;

namespace WaveMP.Service.Dtos;

/// <summary>
/// What the Now Playing view of a player shows.
/// </summary>
[JsonConverter(typeof(StrictStringEnumConverter<PlayerViewModeDto>))]
public enum PlayerViewModeDto
{
    /// <summary>
    /// The cover of the track.
    /// </summary>
    Cover,

    /// <summary>
    /// The video of tracks with a video, muted and following the audio; the cover of tracks without a video.
    /// </summary>
    Video,

    /// <summary>
    /// A visualization of the music.
    /// </summary>
    Visualization,
}
