namespace WaveMP.Core.Jobs.Runs;

/// <summary>
/// The run a job is busy with: the running one, otherwise the oldest queued one.
/// </summary>
public sealed record JobRunActivity(Guid RunId, JobActivityState State);
