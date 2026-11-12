namespace WaveMP.Core.Transcoding.Processes;

/// <summary>
/// Result of a short ffmpeg command.
/// </summary>
/// <param name="Started">Whether ffmpeg could be started at all.</param>
/// <param name="ExitCode">Exit code; <c>-1</c> if it was not started or took too long.</param>
/// <param name="ErrorOutput">The end of the error output.</param>
public sealed record FfmpegCommandResult(bool Started, int ExitCode, string ErrorOutput)
{
    public bool Succeeded => Started && ExitCode == 0;

    public static FfmpegCommandResult NotStarted { get; } = new(false, -1, string.Empty);
}
