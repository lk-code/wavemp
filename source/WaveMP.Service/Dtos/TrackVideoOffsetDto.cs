namespace WaveMP.Service.Dtos;

/// <summary>
/// The offset of the video against the audio of a track.
/// </summary>
public sealed class TrackVideoOffsetDto
{
    /// <summary>
    /// Id of the track.
    /// </summary>
    public required Guid TrackId { get; init; }

    /// <summary>
    /// Offset in milliseconds: position of the video = position of the audio + offset. Positive if the music in the
    /// video starts later than in the audio file (for example after an intro), negative if it starts earlier; 0 without
    /// offset.
    /// </summary>
    public required int OffsetMilliseconds { get; init; }
}
