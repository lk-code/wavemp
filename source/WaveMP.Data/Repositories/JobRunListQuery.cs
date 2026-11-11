using WaveMP.Data.Entities;
using WaveMP.Data.Queries;

namespace WaveMP.Data.Repositories;

/// <summary>
/// Paging, sorting and filter of the runs of a job. Runs are sorted by the time they were requested, ties by id.
/// </summary>
/// <param name="JobId">Id of the job.</param>
/// <param name="Offset">Number of runs to skip.</param>
/// <param name="Limit">Maximum number of runs of the page.</param>
/// <param name="Direction">Sort direction; the newest first by default.</param>
/// <param name="Status">Only runs with this state; <c>null</c> for all.</param>
/// <param name="Trigger">Only runs started this way; <c>null</c> for all.</param>
public sealed record JobRunListQuery(
    string JobId,
    int Offset,
    int Limit,
    SortDirection Direction = SortDirection.Desc,
    JobRunStatus? Status = null,
    JobRunTrigger? Trigger = null);
