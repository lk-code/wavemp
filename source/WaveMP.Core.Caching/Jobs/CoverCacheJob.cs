using Quartz;
using WaveMP.Core.Caching.Covers;

namespace WaveMP.Core.Caching.Jobs;

/// <summary>
/// Prepares the scaled covers as background job, started after scans or by an admin; it has no schedule of its own.
/// Never runs in parallel to itself; the result becomes part of the run history.
/// </summary>
[DisallowConcurrentExecution]
public sealed class CoverCacheJob(ICoverCachePreparer preparer) : IJob
{
    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        context.Result = await preparer.RunAsync(cancellationToken);
    }
}
