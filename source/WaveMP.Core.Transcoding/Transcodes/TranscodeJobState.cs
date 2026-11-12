namespace WaveMP.Core.Transcoding.Transcodes;

/// <summary>
/// State of a transcode.
/// </summary>
public enum TranscodeJobState
{
    /// <summary>
    /// ffmpeg runs.
    /// </summary>
    Running,

    /// <summary>
    /// ffmpeg finished all segments from the start segment on.
    /// </summary>
    Completed,

    /// <summary>
    /// ffmpeg ended with an error.
    /// </summary>
    Failed,

    /// <summary>
    /// The transcode was stopped: idle, replaced by a seek, given up for another one or cancelled by an admin.
    /// </summary>
    Cancelled,
}
