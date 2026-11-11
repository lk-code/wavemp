using WaveMP.Data.Entities;

namespace WaveMP.Core.Jobs.Runs;

/// <summary>
/// An expected failure of a job with a machine-readable code, e.g. a missing media folder. The run history stores
/// the code; the message is an internal English text for the log.
/// </summary>
public sealed class JobFailedException(JobErrorCode code, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public JobErrorCode Code { get; } = code;
}
