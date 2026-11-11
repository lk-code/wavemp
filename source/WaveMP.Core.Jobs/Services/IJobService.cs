using WaveMP.Data.Entities;
using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Jobs.Services;

/// <summary>
/// Overview of the background jobs, their run history and manual starts.
/// </summary>
public interface IJobService
{
    /// <summary>
    /// Whether a job with this id exists.
    /// </summary>
    bool Exists(string jobId);

    /// <summary>
    /// The jobs sorted by id, with their state, schedule, next and last run.
    /// </summary>
    Task<Page<JobOverview>> ListAsync(int offset, int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// A run of the job, or <c>null</c> if it does not exist or belongs to another job.
    /// </summary>
    Task<JobRun?> GetRunAsync(string jobId, Guid runId, CancellationToken cancellationToken = default);

    Task<Page<JobRun>> ListRunsAsync(JobRunListQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queues a run and starts it as soon as possible. Rejected while the job is queued or running.
    /// </summary>
    Task<JobRunStartResult> StartRunAsync(JobRunRequest request, CancellationToken cancellationToken = default);
}
