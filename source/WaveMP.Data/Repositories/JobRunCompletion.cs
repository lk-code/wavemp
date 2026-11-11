using WaveMP.Data.Entities;

namespace WaveMP.Data.Repositories;

/// <summary>
/// How a job run ended.
/// </summary>
/// <param name="Status">Final state.</param>
/// <param name="FinishedAt">When the run ended.</param>
/// <param name="ErrorCode">Reason of a failure.</param>
/// <param name="ErrorDetail">Internal English description of the error, at most 4000 characters.</param>
/// <param name="Result">Result of the job as JSON.</param>
public sealed record JobRunCompletion(
    JobRunStatus Status,
    DateTimeOffset FinishedAt,
    JobErrorCode? ErrorCode = null,
    string? ErrorDetail = null,
    string? Result = null);
