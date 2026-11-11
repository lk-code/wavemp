using WaveMP.Core.Jobs.Definitions;
using WaveMP.Core.Jobs.Runs;
using WaveMP.Data.Entities;

namespace WaveMP.Core.Jobs.Services;

/// <summary>
/// A job with its current state, schedule, next and last run.
/// </summary>
public sealed record JobOverview(
    string JobId,
    JobActivityState State,
    JobSchedule? Schedule,
    DateTimeOffset? NextRunAt,
    JobRun? LastRun);
