namespace WaveMP.Core.Streaming.Decisions;

/// <summary>
/// How a client plays a track.
/// </summary>
public enum PlaybackMethod
{
    /// <summary>
    /// The client plays the file as it is.
    /// </summary>
    DirectPlay,

    /// <summary>
    /// The client cannot play the file; the server has to convert it (roadmap step 12).
    /// </summary>
    Transcode,
}
