using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using WaveMP.Core.Jobs.Definitions;
using WaveMP.Core.Jobs.Runs;
using WaveMP.Core.Jobs.Services;
using WaveMP.Data.Entities;
using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Scanning.Scans;

internal sealed class ScanService(
    IJobService jobs,
    IJobRunTracker tracker,
    IScanProgressTracker progress,
    ILogger<ScanService> logger) : IScanService
{
    /// <summary>
    /// The options the run history writes job results with.
    /// </summary>
    private static readonly JsonSerializerOptions _resultOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public Task<JobRunStartResult> StartAsync(CancellationToken cancellationToken = default)
    {
        return jobs.StartRunAsync(new JobRunRequest(JobIds.LibraryScan, JobRunTrigger.Manual), cancellationToken);
    }

    public async Task<ScanOverview?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        if (tracker.GetActiveRun(JobIds.LibraryScan) is not { } active)
        {
            return null;
        }

        JobRun? run = await jobs.GetRunAsync(JobIds.LibraryScan, active.RunId, cancellationToken);
        return run is null ? null : ToOverview(run);
    }

    public async Task<ScanOverview?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        JobRun? run = await jobs.GetRunAsync(JobIds.LibraryScan, id, cancellationToken);
        return run is null ? null : ToOverview(run);
    }

    public async Task<Page<ScanOverview>> ListAsync(
        int offset,
        int limit,
        SortDirection direction = SortDirection.Desc,
        JobRunStatus? status = null,
        JobRunTrigger? trigger = null,
        CancellationToken cancellationToken = default)
    {
        Page<JobRun> page = await jobs.ListRunsAsync(
            new JobRunListQuery(JobIds.LibraryScan, offset, limit, direction, status, trigger),
            cancellationToken);
        return new Page<ScanOverview>([.. page.Items.Select(ToOverview)], page.Offset, page.Limit, page.Total);
    }

    private ScanOverview ToOverview(JobRun run)
    {
        ScanProgress? current = run.Status is JobRunStatus.Queued or JobRunStatus.Running
            ? progress.Get(run.Id)
            : null;
        return new ScanOverview(run, ReadResult(run), current);
    }

    private ScanResult? ReadResult(JobRun run)
    {
        if (run.Result is null)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ScanResult>(run.Result, _resultOptions);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "The result of scan {RunId} cannot be read", run.Id);
            return null;
        }
    }
}
