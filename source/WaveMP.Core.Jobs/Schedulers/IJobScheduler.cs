using WaveMP.Core.Jobs.Definitions;

namespace WaveMP.Core.Jobs.Schedulers;

/// <summary>
/// Plans the jobs in the Quartz scheduler (in memory, nothing is persisted by Quartz).
/// </summary>
public interface IJobScheduler
{
    /// <summary>
    /// Adds all registered jobs and creates their schedules. Runs on start.
    /// </summary>
    Task RegisterAndScheduleAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the schedule of the job with its current one, e.g. after its settings changed. The next run is one
    /// interval from now.
    /// </summary>
    Task RescheduleAsync(string jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// When the schedule of the job fires next, or <c>null</c> without a schedule.
    /// </summary>
    Task<DateTimeOffset?> GetNextRunAsync(string jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts the job now with the job data; the run waits while another run of the job is running.
    /// </summary>
    Task TriggerAsync(
        string jobId,
        IReadOnlyDictionary<string, string> data,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The schedule provider of the job, or <c>null</c> if it only runs when started.
    /// </summary>
    IJobScheduleProvider? FindScheduleProvider(string jobId);
}
