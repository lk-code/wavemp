using WaveMP.Core.Jobs.Definitions;
using WaveMP.Core.Jobs.Runs;
using WaveMP.Core.Jobs.Services;
using WaveMP.Data.Entities;
using WaveMP.Service.Dtos;

namespace WaveMP.Service.Mappings;

public static class JobMappingExtensions
{
    public static JobDto ToDto(this JobOverview job)
    {
        return new JobDto
        {
            Id = job.JobId.ToJobIdDto(),
            State = job.State.ToDto(),
            IntervalMinutes = job.Schedule is { } schedule ? (int)schedule.Interval.TotalMinutes : null,
            NextRunAt = job.NextRunAt,
            LastRun = job.LastRun?.ToDto(),
        };
    }

    public static JobRunDto ToDto(this JobRun run)
    {
        return new JobRunDto
        {
            Id = run.Id,
            JobId = run.JobId.ToJobIdDto(),
            Trigger = run.Trigger.ToDto(),
            Status = run.Status.ToDto(),
            QueuedAt = run.QueuedAt,
            StartedAt = run.StartedAt,
            FinishedAt = run.FinishedAt,
            DurationMilliseconds = run.DurationMilliseconds(),
            ErrorCode = run.ErrorCode?.ToDto(),
        };
    }

    /// <summary>
    /// Duration of a finished run in milliseconds.
    /// </summary>
    public static long? DurationMilliseconds(this JobRun run)
    {
        return run is { StartedAt: { } started, FinishedAt: { } finished }
            ? (long)Math.Max(0, Math.Round((finished - started).TotalMilliseconds))
            : null;
    }

    public static JobIdDto ToJobIdDto(this string jobId)
    {
        return jobId switch
        {
            JobIds.LibraryScan => JobIdDto.LibraryScan,
            _ => throw new ArgumentOutOfRangeException(nameof(jobId), jobId, "Unknown job."),
        };
    }

    public static JobStateDto ToDto(this JobActivityState state)
    {
        return state switch
        {
            JobActivityState.Idle => JobStateDto.Idle,
            JobActivityState.Queued => JobStateDto.Queued,
            JobActivityState.Running => JobStateDto.Running,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown job state."),
        };
    }

    public static JobTriggerDto ToDto(this JobRunTrigger trigger)
    {
        return trigger switch
        {
            JobRunTrigger.Schedule => JobTriggerDto.Schedule,
            JobRunTrigger.Manual => JobTriggerDto.Manual,
            JobRunTrigger.FileWatcher => JobTriggerDto.FileWatcher,
            _ => throw new ArgumentOutOfRangeException(nameof(trigger), trigger, "Unknown trigger."),
        };
    }

    public static JobRunTrigger ToEntity(this JobTriggerDto trigger)
    {
        return trigger switch
        {
            JobTriggerDto.Schedule => JobRunTrigger.Schedule,
            JobTriggerDto.Manual => JobRunTrigger.Manual,
            JobTriggerDto.FileWatcher => JobRunTrigger.FileWatcher,
            _ => throw new ArgumentOutOfRangeException(nameof(trigger), trigger, "Unknown trigger."),
        };
    }

    public static JobRunStatusDto ToDto(this JobRunStatus status)
    {
        return status switch
        {
            JobRunStatus.Queued => JobRunStatusDto.Queued,
            JobRunStatus.Running => JobRunStatusDto.Running,
            JobRunStatus.Succeeded => JobRunStatusDto.Succeeded,
            JobRunStatus.Failed => JobRunStatusDto.Failed,
            JobRunStatus.Canceled => JobRunStatusDto.Canceled,
            JobRunStatus.Interrupted => JobRunStatusDto.Interrupted,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown run status."),
        };
    }

    public static JobRunStatus ToEntity(this JobRunStatusDto status)
    {
        return status switch
        {
            JobRunStatusDto.Queued => JobRunStatus.Queued,
            JobRunStatusDto.Running => JobRunStatus.Running,
            JobRunStatusDto.Succeeded => JobRunStatus.Succeeded,
            JobRunStatusDto.Failed => JobRunStatus.Failed,
            JobRunStatusDto.Canceled => JobRunStatus.Canceled,
            JobRunStatusDto.Interrupted => JobRunStatus.Interrupted,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown run status."),
        };
    }

    public static JobErrorCodeDto ToDto(this JobErrorCode code)
    {
        return code switch
        {
            JobErrorCode.Failed => JobErrorCodeDto.JobFailed,
            JobErrorCode.Interrupted => JobErrorCodeDto.JobInterrupted,
            JobErrorCode.MediaFolderNotFound => JobErrorCodeDto.ScanMediaFolderNotFound,
            JobErrorCode.MediaFolderUnreadable => JobErrorCodeDto.ScanMediaFolderUnreadable,
            JobErrorCode.MediaFolderEmpty => JobErrorCodeDto.ScanMediaFolderEmpty,
            JobErrorCode.ProbeUnavailable => JobErrorCodeDto.ScanProbeUnavailable,
            _ => throw new ArgumentOutOfRangeException(nameof(code), code, "Unknown error code."),
        };
    }
}
