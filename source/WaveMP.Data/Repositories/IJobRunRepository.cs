using WaveMP.Data.Entities;
using WaveMP.Data.Queries;

namespace WaveMP.Data.Repositories;

/// <summary>
/// Access to the run history of the background jobs. Nothing is tracked.
/// </summary>
public interface IJobRunRepository
{
    Task AddAsync(JobRun run, CancellationToken cancellationToken = default);

    Task<JobRun?> FindAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// The newest run of each of the jobs, by the time they were requested; jobs without runs are missing.
    /// </summary>
    Task<IReadOnlyDictionary<string, JobRun>> FindLatestAsync(
        IReadOnlyCollection<string> jobIds,
        CancellationToken cancellationToken = default);

    Task<Page<JobRun>> ListAsync(JobRunListQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a queued run as running. Returns <c>false</c> if there is no such run.
    /// </summary>
    Task<bool> MarkRunningAsync(Guid id, DateTimeOffset startedAt, CancellationToken cancellationToken = default);

    Task CompleteAsync(Guid id, JobRunCompletion completion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks all queued and running runs as interrupted, e.g. on start after the service stopped unexpectedly.
    /// Returns their number.
    /// </summary>
    Task<int> MarkUnfinishedAsInterruptedAsync(DateTimeOffset now, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the finished runs of the job that were requested before <paramref name="cutoff"/> or are not among the
    /// <paramref name="keep"/> newest runs. Queued and running runs are always kept. Returns the number deleted.
    /// </summary>
    Task<int> DeleteHistoryAsync(
        string jobId,
        DateTimeOffset cutoff,
        int keep,
        CancellationToken cancellationToken = default);
}
