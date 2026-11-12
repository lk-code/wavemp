namespace WaveMP.Core.Streaming.Offsets;

/// <summary>
/// The offset of the video against the audio of a track: position of the video = position of the audio + offset.
/// Players that play the audio and show the muted video synchronize the video with it. It is stored in the database,
/// because videos are never written.
/// </summary>
public interface IVideoOffsetService
{
    /// <summary>
    /// The offset of the video of the track; 0 if none is set.
    /// </summary>
    Task<VideoOffsetResult> GetAsync(Guid trackId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the offset of the video of the track; 0 removes it. Only tracks with a video have an offset.
    /// </summary>
    Task<VideoOffsetResult> SetAsync(Guid trackId, int milliseconds, CancellationToken cancellationToken = default);
}
