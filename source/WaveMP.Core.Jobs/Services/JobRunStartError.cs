namespace WaveMP.Core.Jobs.Services;

/// <summary>
/// Why a run was not started.
/// </summary>
public enum JobRunStartError
{
    /// <summary>
    /// There is no job with this id.
    /// </summary>
    NotFound,

    /// <summary>
    /// The job is already queued or running.
    /// </summary>
    AlreadyActive,
}
