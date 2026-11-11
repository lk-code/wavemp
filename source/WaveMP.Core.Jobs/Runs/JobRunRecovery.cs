using Microsoft.Extensions.Logging;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Jobs.Runs;

/// <summary>
/// Marks the runs that were queued or running when the service stopped as interrupted. Runs on start, before any job
/// can run.
/// </summary>
public sealed class JobRunRecovery(
    IJobRunRepository runs,
    TimeProvider timeProvider,
    ILogger<JobRunRecovery> logger)
{
    public async Task RecoverAsync(CancellationToken cancellationToken = default)
    {
        int interrupted = await runs.MarkUnfinishedAsInterruptedAsync(timeProvider.GetUtcNow(), cancellationToken);
        if (interrupted > 0)
        {
            logger.LogWarning("Marked {RunCount} unfinished job run(s) as interrupted", interrupted);
        }
    }
}
