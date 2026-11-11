using System.Diagnostics.CodeAnalysis;
using WaveMP.Data.Entities;

namespace WaveMP.Core.Jobs.Services;

/// <summary>
/// Result of starting a run: the queued run or the reason why none was started.
/// </summary>
public sealed record JobRunStartResult(JobRun? Run, JobRunStartError? Error)
{
    [MemberNotNullWhen(true, nameof(Run))]
    public bool IsSuccess => Run is not null;

    public static JobRunStartResult Success(JobRun run)
    {
        return new JobRunStartResult(run, null);
    }

    public static JobRunStartResult Failure(JobRunStartError error)
    {
        return new JobRunStartResult(null, error);
    }
}
