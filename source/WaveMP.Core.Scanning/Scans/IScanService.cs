using WaveMP.Core.Jobs.Services;
using WaveMP.Data.Entities;
using WaveMP.Data.Queries;

namespace WaveMP.Core.Scanning.Scans;

/// <summary>
/// The library scan for the API: start it, follow the current run and read the history.
/// </summary>
public interface IScanService
{
    /// <summary>
    /// Starts a scan of the whole media folder. Rejected while a scan is queued or running.
    /// </summary>
    Task<JobRunStartResult> StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The queued or running scan with its progress, or <c>null</c> if no scan is active.
    /// </summary>
    Task<ScanOverview?> GetCurrentAsync(CancellationToken cancellationToken = default);

    Task<ScanOverview?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// The scans by the time they were requested, newest first by default.
    /// </summary>
    Task<Page<ScanOverview>> ListAsync(
        int offset,
        int limit,
        SortDirection direction = SortDirection.Desc,
        JobRunStatus? status = null,
        JobRunTrigger? trigger = null,
        CancellationToken cancellationToken = default);
}
