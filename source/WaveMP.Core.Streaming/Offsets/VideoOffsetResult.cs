namespace WaveMP.Core.Streaming.Offsets;

/// <summary>
/// The offset of the video of a track, or why there is none.
/// </summary>
/// <param name="Status">Whether the offset was found or set.</param>
/// <param name="TrackId">Id of the track.</param>
/// <param name="OffsetMilliseconds">Offset of the video against the audio in milliseconds; 0 unless found.</param>
public sealed record VideoOffsetResult(VideoOffsetStatus Status, Guid TrackId, int OffsetMilliseconds = 0)
{
    public bool IsFound => Status == VideoOffsetStatus.Found;
}
