using System.ComponentModel.DataAnnotations;
using WaveMP.Core.Streaming.Offsets;

namespace WaveMP.Service.Dtos;

/// <summary>
/// Sets the offset of the video against the audio of a track.
/// </summary>
public sealed class UpdateTrackVideoOffsetDto
{
    /// <summary>
    /// Offset in milliseconds from -600000 to 600000: position of the video = position of the audio + offset. Positive
    /// if the music in the video starts later than in the audio file (for example after an intro), negative if it
    /// starts earlier; 0 removes the offset.
    /// </summary>
    [Required]
    [Range(VideoOffsets.MinMilliseconds, VideoOffsets.MaxMilliseconds)]
    public required int? OffsetMilliseconds { get; init; }
}
