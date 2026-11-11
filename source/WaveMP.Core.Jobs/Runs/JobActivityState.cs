namespace WaveMP.Core.Jobs.Runs;

/// <summary>
/// What a job is doing right now.
/// </summary>
public enum JobActivityState
{
    Idle,

    /// <summary>
    /// A run was requested and waits to start.
    /// </summary>
    Queued,

    Running,
}
