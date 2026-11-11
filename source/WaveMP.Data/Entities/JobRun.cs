namespace WaveMP.Data.Entities;

/// <summary>
/// One run of a background job (e.g. the library scan): when and why it ran, how it ended and its result. Written
/// by the job listener; old runs are removed automatically.
/// </summary>
public class JobRun
{
    public Guid Id { get; set; }

    /// <summary>
    /// Stable id of the job, e.g. <c>library-scan</c>.
    /// </summary>
    public required string JobId { get; set; }

    public required JobRunTrigger Trigger { get; set; }

    public required JobRunStatus Status { get; set; }

    /// <summary>
    /// When the run was requested; for scheduled runs the same as <see cref="StartedAt"/>.
    /// </summary>
    public required DateTimeOffset QueuedAt { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? FinishedAt { get; set; }

    /// <summary>
    /// Why the run failed, for <see cref="JobRunStatus.Failed"/> and <see cref="JobRunStatus.Interrupted"/>.
    /// </summary>
    public JobErrorCode? ErrorCode { get; set; }

    /// <summary>
    /// Internal English description of the error for the log and support; never shown in the user interface.
    /// </summary>
    public string? ErrorDetail { get; set; }

    /// <summary>
    /// Result of the job as JSON (column type <c>jsonb</c>), e.g. the statistics of a scan.
    /// </summary>
    public string? Result { get; set; }
}
