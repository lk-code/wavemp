using WaveMP.Data.Entities;

namespace WaveMP.Core.Jobs.Services;

/// <summary>
/// Starts a run of a job now.
/// </summary>
/// <param name="JobId">Id of the job.</param>
/// <param name="Trigger">What started the run.</param>
/// <param name="Payload">Job specific input as JSON, e.g. the folders to scan.</param>
public sealed record JobRunRequest(string JobId, JobRunTrigger Trigger, string? Payload = null);
