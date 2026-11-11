namespace WaveMP.Data.Entities;

/// <summary>
/// State of a job run. Stored as its name.
/// </summary>
public enum JobRunStatus
{
    /// <summary>
    /// Requested and waiting to start.
    /// </summary>
    Queued,

    Running,

    Succeeded,

    Failed,

    /// <summary>
    /// Stopped before it ended, e.g. because the service shut down.
    /// </summary>
    Canceled,

    /// <summary>
    /// The service stopped unexpectedly while the run was queued or running.
    /// </summary>
    Interrupted,
}
