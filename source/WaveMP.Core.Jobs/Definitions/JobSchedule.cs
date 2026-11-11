namespace WaveMP.Core.Jobs.Definitions;

/// <summary>
/// The periodic schedule of a job.
/// </summary>
/// <param name="Interval">Time between two runs.</param>
/// <param name="FirstRunDelay">Time until the first run after the schedule was created.</param>
public sealed record JobSchedule(TimeSpan Interval, TimeSpan FirstRunDelay);
