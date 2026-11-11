namespace WaveMP.Service.Dtos;

/// <summary>
/// One run of a background job.
/// </summary>
public sealed class JobRunDto
{
    public required Guid Id { get; init; }

    public required JobIdDto JobId { get; init; }

    public required JobTriggerDto Trigger { get; init; }

    public required JobRunStatusDto Status { get; init; }

    /// <summary>
    /// When the run was requested.
    /// </summary>
    public required DateTimeOffset QueuedAt { get; init; }

    public DateTimeOffset? StartedAt { get; init; }

    public DateTimeOffset? FinishedAt { get; init; }

    /// <summary>
    /// Duration of a finished run in milliseconds.
    /// </summary>
    public long? DurationMilliseconds { get; init; }

    /// <summary>
    /// Why the run failed or was interrupted.
    /// </summary>
    public JobErrorCodeDto? ErrorCode { get; init; }
}
