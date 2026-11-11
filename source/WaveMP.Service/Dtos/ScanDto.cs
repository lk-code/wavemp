namespace WaveMP.Service.Dtos;

/// <summary>
/// A scan of the media folder: its run, its progress while running and its result once finished.
/// </summary>
public sealed class ScanDto
{
    public required Guid Id { get; init; }

    public required JobTriggerDto Trigger { get; init; }

    public required JobRunStatusDto Status { get; init; }

    /// <summary>
    /// When the scan was requested.
    /// </summary>
    public required DateTimeOffset QueuedAt { get; init; }

    public DateTimeOffset? StartedAt { get; init; }

    public DateTimeOffset? FinishedAt { get; init; }

    /// <summary>
    /// Duration of a finished scan in milliseconds.
    /// </summary>
    public long? DurationMilliseconds { get; init; }

    /// <summary>
    /// Why the scan failed or was interrupted.
    /// </summary>
    public JobErrorCodeDto? ErrorCode { get; init; }

    /// <summary>
    /// Only some folders were scanned (after changes found by the file watcher).
    /// </summary>
    public required bool IsPartial { get; init; }

    /// <summary>
    /// Progress while the scan is running.
    /// </summary>
    public ScanProgressDto? Progress { get; init; }

    /// <summary>
    /// Statistics of a finished scan.
    /// </summary>
    public ScanStatisticsDto? Statistics { get; init; }

    /// <summary>
    /// Files with problems (at most 50), empty while running.
    /// </summary>
    public required IReadOnlyList<ScanIssueDto> Issues { get; init; }
}
