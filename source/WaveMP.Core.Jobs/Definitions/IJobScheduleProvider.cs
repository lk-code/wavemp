namespace WaveMP.Core.Jobs.Definitions;

/// <summary>
/// Provides the periodic schedule of a job from code and settings. Jobs without a provider only run when started.
/// </summary>
public interface IJobScheduleProvider
{
    string JobId { get; }

    /// <summary>
    /// The current schedule, or <c>null</c> if the job should only run when started.
    /// </summary>
    JobSchedule? GetSchedule(JobScheduleReason reason);
}
