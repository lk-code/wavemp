namespace WaveMP.Core.Transcoding.Processes;

/// <summary>
/// Runs short ffmpeg commands to the end, e.g. <c>-version</c> or the test encodes of the hardware detection.
/// </summary>
public interface IFfmpegCommandRunner
{
    /// <summary>
    /// Runs ffmpeg with the arguments; a command that takes longer than <paramref name="timeout"/> is stopped and
    /// fails.
    /// </summary>
    Task<FfmpegCommandResult> RunAsync(
        IReadOnlyList<string> arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}
