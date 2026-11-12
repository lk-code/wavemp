namespace WaveMP.Core.Transcoding.Processes;

/// <summary>
/// A running ffmpeg process of a transcode.
/// </summary>
public interface IFfmpegProcess : IDisposable
{
    /// <summary>
    /// Completes with the exit code when the process has ended.
    /// </summary>
    Task<int> Exited { get; }

    /// <summary>
    /// The end of the error output, for the log.
    /// </summary>
    string ErrorOutput { get; }

    /// <summary>
    /// Stops the process with all its children; nothing happens if it has ended.
    /// </summary>
    void Kill();
}
