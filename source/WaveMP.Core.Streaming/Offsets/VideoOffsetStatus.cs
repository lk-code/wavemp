namespace WaveMP.Core.Streaming.Offsets;

/// <summary>
/// Result of reading or setting the offset of the video of a track.
/// </summary>
public enum VideoOffsetStatus
{
    Found,

    /// <summary>
    /// The track does not exist.
    /// </summary>
    TrackNotFound,

    /// <summary>
    /// The track has no video.
    /// </summary>
    NoVideo,

    /// <summary>
    /// The offset lies outside of <see cref="VideoOffsets.MinMilliseconds"/> and
    /// <see cref="VideoOffsets.MaxMilliseconds"/>.
    /// </summary>
    Invalid,
}
