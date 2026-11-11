namespace WaveMP.Core.Jobs.Definitions;

/// <summary>
/// Keys of the job data that a run started through <see cref="Services.IJobService"/> carries.
/// </summary>
public static class JobDataKeys
{
    /// <summary>
    /// Id of the run in the run history.
    /// </summary>
    public const string RunId = "wavemp.run-id";

    /// <summary>
    /// What started the run (<see cref="Data.Entities.JobRunTrigger"/> as its name).
    /// </summary>
    public const string Trigger = "wavemp.trigger";

    /// <summary>
    /// Job specific input as JSON, e.g. the folders to scan.
    /// </summary>
    public const string Payload = "wavemp.payload";
}
