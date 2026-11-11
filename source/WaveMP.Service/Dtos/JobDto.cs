namespace WaveMP.Service.Dtos;

/// <summary>
/// A background job with its schedule, its next and its last run.
/// </summary>
public sealed class JobDto
{
    public required JobIdDto Id { get; init; }

    public required JobStateDto State { get; init; }

    /// <summary>
    /// Minutes between two scheduled runs; null for jobs that only run when started.
    /// </summary>
    public int? IntervalMinutes { get; init; }

    /// <summary>
    /// When the schedule runs the job next.
    /// </summary>
    public DateTimeOffset? NextRunAt { get; init; }

    /// <summary>
    /// The most recent run, also while it is queued or running.
    /// </summary>
    public JobRunDto? LastRun { get; init; }
}
