namespace WaveMP.Core.Metadata.Probes;

/// <summary>
/// ffprobe cannot be started, e.g. because it is not installed.
/// </summary>
public sealed class ProbeUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);
