namespace WaveMP.Core.Streaming.Sources;

/// <summary>
/// Whether the file of a track can be delivered.
/// </summary>
public enum StreamSourceStatus
{
    Found,

    /// <summary>
    /// The track does not exist.
    /// </summary>
    TrackNotFound,

    /// <summary>
    /// The track has no audio file (for the audio stream), or neither an audio file nor a video (for the playback of
    /// its sound).
    /// </summary>
    NoAudio,

    /// <summary>
    /// The track has no video.
    /// </summary>
    NoVideo,

    /// <summary>
    /// The file of the track is gone since the last scan, or its stored path leaves the media folder.
    /// </summary>
    FileMissing,
}
