using Microsoft.EntityFrameworkCore;
using WaveMP.Data.Entities;
using WaveMP.Data.Queries;

namespace WaveMP.Data.Repositories;

internal sealed class JobRunRepository(WaveMpDbContext dbContext) : IJobRunRepository
{
    public async Task AddAsync(JobRun run, CancellationToken cancellationToken = default)
    {
        dbContext.JobRuns.Add(run);
        await dbContext.SaveChangesAsync(cancellationToken);
        dbContext.Entry(run).State = EntityState.Detached;
    }

    public Task<JobRun?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.JobRuns.AsNoTracking().FirstOrDefaultAsync(run => run.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, JobRun>> FindLatestAsync(
        IReadOnlyCollection<string> jobIds,
        CancellationToken cancellationToken = default)
    {
        Dictionary<string, JobRun> latest = new(StringComparer.Ordinal);
        foreach (string jobId in jobIds)
        {
            JobRun? run = await dbContext.JobRuns.AsNoTracking()
                .Where(run => run.JobId == jobId)
                .OrderByDescending(run => run.QueuedAt)
                .ThenByDescending(run => run.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (run is not null)
            {
                latest[jobId] = run;
            }
        }

        return latest;
    }

    public Task<Page<JobRun>> ListAsync(JobRunListQuery query, CancellationToken cancellationToken = default)
    {
        IQueryable<JobRun> runs = dbContext.JobRuns.AsNoTracking().Where(run => run.JobId == query.JobId);
        if (query.Status is { } status)
        {
            runs = runs.Where(run => run.Status == status);
        }

        if (query.Trigger is { } trigger)
        {
            runs = runs.Where(run => run.Trigger == trigger);
        }

        return runs
            .OrderBy(run => run.QueuedAt, query.Direction)
            .ThenBy(run => run.Id, query.Direction)
            .ToPageAsync(query.Offset, query.Limit, cancellationToken);
    }

    public async Task<bool> MarkRunningAsync(
        Guid id,
        DateTimeOffset startedAt,
        CancellationToken cancellationToken = default)
    {
        int updated = await dbContext.JobRuns
            .Where(run => run.Id == id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(run => run.Status, JobRunStatus.Running)
                    .SetProperty(run => run.StartedAt, startedAt),
                cancellationToken);
        return updated > 0;
    }

    public Task CompleteAsync(Guid id, JobRunCompletion completion, CancellationToken cancellationToken = default)
    {
        return dbContext.JobRuns
            .Where(run => run.Id == id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(run => run.Status, completion.Status)
                    .SetProperty(run => run.FinishedAt, completion.FinishedAt)
                    .SetProperty(run => run.ErrorCode, completion.ErrorCode)
                    .SetProperty(run => run.ErrorDetail, completion.ErrorDetail)
                    .SetProperty(run => run.Result, completion.Result),
                cancellationToken);
    }

    public Task<int> MarkUnfinishedAsInterruptedAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        return dbContext.JobRuns
            .Where(run => run.Status == JobRunStatus.Queued || run.Status == JobRunStatus.Running)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(run => run.Status, JobRunStatus.Interrupted)
                    .SetProperty(run => run.ErrorCode, JobErrorCode.Interrupted)
                    .SetProperty(run => run.FinishedAt, now),
                cancellationToken);
    }

    public Task<int> DeleteHistoryAsync(
        string jobId,
        DateTimeOffset cutoff,
        int keep,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Guid> newest = dbContext.JobRuns
            .Where(run => run.JobId == jobId)
            .OrderByDescending(run => run.QueuedAt)
            .ThenByDescending(run => run.Id)
            .Take(keep)
            .Select(run => run.Id);

        return dbContext.JobRuns
            .Where(run => run.JobId == jobId
                && run.Status != JobRunStatus.Queued
                && run.Status != JobRunStatus.Running
                && (run.QueuedAt < cutoff || !newest.Contains(run.Id)))
            .ExecuteDeleteAsync(cancellationToken);
    }
}
