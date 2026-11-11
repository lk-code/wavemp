namespace WaveMP.Core.Streaming.Sources;

/// <summary>
/// Whether the audio file of a track can be delivered.
/// </summary>
public enum StreamSourceStatus
{
    Found,

    /// <summary>
    /// The track does not exist.
    /// </summary>
    TrackNotFound,

    /// <summary>
    /// The track has no audio file, for example a music video only.
    /// </summary>
    NoAudio,

    /// <summary>
    /// The audio file of the track is gone since the last scan, or its stored path leaves the media folder.
    /// </summary>
    FileMissing,
}
