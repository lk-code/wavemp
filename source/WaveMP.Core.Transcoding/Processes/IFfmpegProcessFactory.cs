namespace WaveMP.Core.Transcoding.Processes;

/// <summary>
/// Starts ffmpeg as a separate process (never linked); tests replace it.
/// </summary>
public interface IFfmpegProcessFactory
{
    /// <summary>
    /// Starts ffmpeg with the arguments, one by one without a shell. Throws <see cref="FfmpegStartException"/> if
    /// ffmpeg cannot be started.
    /// </summary>
    IFfmpegProcess Start(IReadOnlyList<string> arguments);
}
