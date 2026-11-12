namespace WaveMP.Core.Transcoding.Processes;

/// <summary>
/// ffmpeg could not be started, e.g. because it is not installed.
/// </summary>
public sealed class FfmpegStartException(string message, Exception? innerException = null)
    : Exception(message, innerException);
