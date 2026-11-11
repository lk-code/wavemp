using WaveMP.Core.Jobs.Definitions;
using WaveMP.Core.Jobs.Runs;
using WaveMP.Core.Jobs.Schedulers;
using WaveMP.Data.Entities;
using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Jobs.Services;

internal sealed class JobService(
    IEnumerable<JobRegistration> registrations,
    IJobRunTracker tracker,
    IJobScheduler scheduler,
    IJobRunRepository runs,
    TimeProvider timeProvider) : IJobService
{
    private readonly string[] _jobIds = [.. registrations
        .Select(registration => registration.JobId)
        .Distinct(StringComparer.Ordinal)
        .Order(StringComparer.Ordinal)];

    public bool Exists(string jobId)
    {
        return _jobIds.Contains(jobId, StringComparer.Ordinal);
    }

    public async Task<Page<JobOverview>> ListAsync(int offset, int limit, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);

        string[] jobIds = [.. _jobIds.Skip(offset).Take(limit)];
        IReadOnlyDictionary<string, JobRun> lastRuns = await runs.FindLatestAsync(jobIds, cancellationToken);

        List<JobOverview> items = [];
        foreach (string jobId in jobIds)
        {
            JobSchedule? schedule = scheduler.FindScheduleProvider(jobId)?.GetSchedule(JobScheduleReason.SettingsChanged);
            items.Add(new JobOverview(
                jobId,
                tracker.GetState(jobId),
                schedule,
                await scheduler.GetNextRunAsync(jobId, cancellationToken),
                lastRuns.GetValueOrDefault(jobId)));
        }

        return new Page<JobOverview>(items, offset, limit, _jobIds.Length);
    }

    public async Task<JobRun?> GetRunAsync(string jobId, Guid runId, CancellationToken cancellationToken = default)
    {
        JobRun? run = await runs.FindAsync(runId, cancellationToken);
        return run?.JobId == jobId ? run : null;
    }

    public Task<Page<JobRun>> ListRunsAsync(JobRunListQuery query, CancellationToken cancellationToken = default)
    {
        return runs.ListAsync(query, cancellationToken);
    }

    public async Task<JobRunStartResult> StartRunAsync(
        JobRunRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!Exists(request.JobId))
        {
            return JobRunStartResult.Failure(JobRunStartError.NotFound);
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        var runId = Guid.CreateVersion7(now);
        if (!tracker.TryReserve(request.JobId, runId))
        {
            return JobRunStartResult.Failure(JobRunStartError.AlreadyActive);
        }

        JobRun run = new()
        {
            Id = runId,
            JobId = request.JobId,
            Trigger = request.Trigger,
            Status = JobRunStatus.Queued,
            QueuedAt = now,
        };

        try
        {
            await runs.AddAsync(run, cancellationToken);

            Dictionary<string, string> data = new()
            {
                [JobDataKeys.RunId] = runId.ToString(),
                [JobDataKeys.Trigger] = request.Trigger.ToString(),
            };
            if (request.Payload is not null)
            {
                data[JobDataKeys.Payload] = request.Payload;
            }

            await scheduler.TriggerAsync(request.JobId, data, cancellationToken);
        }
        catch (Exception exception)
        {
            tracker.Release(request.JobId, runId);
            await TryMarkFailedAsync(runId, exception);
            throw;
        }

        return JobRunStartResult.Success(run);
    }

    private async Task TryMarkFailedAsync(Guid runId, Exception exception)
    {
        try
        {
            await runs.CompleteAsync(
                runId,
                new JobRunCompletion(
                    JobRunStatus.Failed,
                    timeProvider.GetUtcNow(),
                    JobErrorCode.Failed,
                    $"{exception.GetType().FullName}: {exception.Message}"),
                CancellationToken.None);
        }
        catch (Exception)
        {
            // The run stays queued and is marked as interrupted on the next start.
        }
    }
}
