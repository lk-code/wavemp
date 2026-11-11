namespace WaveMP.Core.Jobs.Runs;

/// <summary>
/// Knows which runs of each job are queued or running in this process. Jobs never run in parallel to themselves, so
/// at most one run per job is running.
/// </summary>
public interface IJobRunTracker
{
    JobActivityState GetState(string jobId);

    /// <summary>
    /// The running run of the job, otherwise its oldest queued run, otherwise <c>null</c>.
    /// </summary>
    JobRunActivity? GetActiveRun(string jobId);

    /// <summary>
    /// Queues a run if the job is idle. Returns <c>false</c> if the job is queued or running; checking and queuing
    /// happen atomically.
    /// </summary>
    bool TryReserve(string jobId, Guid runId);

    /// <summary>
    /// Marks the run as running for the firing of Quartz with the id <paramref name="fireInstanceId"/>.
    /// </summary>
    void MarkRunning(string jobId, Guid runId, string fireInstanceId);

    /// <summary>
    /// The run of a firing that is running, or <c>null</c>.
    /// </summary>
    Guid? FindRunId(string fireInstanceId);

    /// <summary>
    /// Removes a queued or running run.
    /// </summary>
    void Release(string jobId, Guid runId);
}
