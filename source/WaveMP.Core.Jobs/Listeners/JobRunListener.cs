using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using WaveMP.Core.Jobs.Definitions;
using WaveMP.Core.Jobs.Options;
using WaveMP.Core.Jobs.Runs;
using WaveMP.Data.Entities;
using WaveMP.Data.Repositories;
using JobRunStatus = WaveMP.Data.Entities.JobRunStatus;

namespace WaveMP.Core.Jobs.Listeners;

/// <summary>
/// Writes the run history of all jobs: a run is marked as running when it starts and completed with its result or
/// error when it ends, after which old runs of the job are removed. Errors of the history are logged and never
/// affect the job.
/// </summary>
internal sealed class JobRunListener(
    IServiceScopeFactory scopeFactory,
    IJobRunTracker tracker,
    TimeProvider timeProvider,
    IOptions<JobHistoryOptions> options,
    ILogger<JobRunListener> logger) : IJobListener
{
    internal const int MaxErrorDetailLength = 4000;

    internal static readonly JsonSerializerOptions ResultJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public string Name => "WaveMP.JobRunHistory";

    public async ValueTask JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        string jobId = context.JobDetail.Key.Name;
        DateTimeOffset now = timeProvider.GetUtcNow();
        JobDataMap data = context.MergedJobDataMap;
        Guid runId = ReadRunId(data) ?? Guid.CreateVersion7(now);
        JobRunTrigger trigger = ReadTrigger(data);
        tracker.MarkRunning(jobId, runId, context.FireInstanceId);

        try
        {
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            IJobRunRepository runs = scope.ServiceProvider.GetRequiredService<IJobRunRepository>();
            if (!await runs.MarkRunningAsync(runId, now, CancellationToken.None))
            {
                await runs.AddAsync(
                    new JobRun
                    {
                        Id = runId,
                        JobId = jobId,
                        Trigger = trigger,
                        Status = JobRunStatus.Running,
                        QueuedAt = now,
                        StartedAt = now,
                    },
                    CancellationToken.None);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Cannot record the start of run {RunId} of job {JobId}", runId, jobId);
        }
    }

    public ValueTask JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        return CompleteAsync(
            context,
            new JobRunCompletion(JobRunStatus.Canceled, timeProvider.GetUtcNow()));
    }

    public ValueTask JobWasExecuted(
        IJobExecutionContext context,
        JobExecutionException? jobException,
        CancellationToken cancellationToken)
    {
        bool canceled = context.Outcome == ExecutionOutcome.Cancelled;
        return CompleteAsync(context, Classify(jobException, canceled, context.Result, timeProvider.GetUtcNow()));
    }

    public ValueTask JobProgressChanged(
        IJobExecutionContext context,
        Quartz.Extensibility.FireInstanceProgress progress,
        CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// How a run ended: canceled if it was canceled anywhere in the exception chain, failed with the code of a
    /// <see cref="JobFailedException"/> or <see cref="JobErrorCode.Failed"/> for every other exception, otherwise
    /// succeeded with the result as JSON.
    /// </summary>
    internal static JobRunCompletion Classify(
        Exception? exception,
        bool canceled,
        object? result,
        DateTimeOffset finishedAt)
    {
        string? resultJson = result is null
            ? null
            : JsonSerializer.Serialize(result, result.GetType(), ResultJsonOptions);

        List<Exception> chain = [];
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            chain.Add(current);
        }

        if (canceled || chain.Any(item => item is OperationCanceledException))
        {
            return new JobRunCompletion(JobRunStatus.Canceled, finishedAt, Result: resultJson);
        }

        if (chain.Count == 0)
        {
            return new JobRunCompletion(JobRunStatus.Succeeded, finishedAt, Result: resultJson);
        }

        Exception cause = chain.FirstOrDefault(item => item is JobFailedException)
            ?? chain.LastOrDefault(item => item is not JobExecutionException)
            ?? chain[^1];
        JobErrorCode code = cause is JobFailedException failed ? failed.Code : JobErrorCode.Failed;
        string detail = $"{cause.GetType().FullName}: {cause.Message}";
        if (detail.Length > MaxErrorDetailLength)
        {
            detail = detail[..MaxErrorDetailLength];
        }

        return new JobRunCompletion(JobRunStatus.Failed, finishedAt, code, detail, resultJson);
    }

    private async ValueTask CompleteAsync(IJobExecutionContext context, JobRunCompletion completion)
    {
        string jobId = context.JobDetail.Key.Name;
        Guid? runId = tracker.FindRunId(context.FireInstanceId) ?? ReadRunId(context.MergedJobDataMap);
        if (runId is not { } id)
        {
            logger.LogWarning("Run of job {JobId} ended without a known run id", jobId);
            return;
        }

        try
        {
            if (completion.Status == JobRunStatus.Failed)
            {
                logger.LogWarning(
                    "Run {RunId} of job {JobId} failed with {ErrorCode}: {ErrorDetail}",
                    id,
                    jobId,
                    completion.ErrorCode,
                    completion.ErrorDetail);
            }

            // Written even while the service shuts down, so that a canceled run is not reported as interrupted.
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            IJobRunRepository runs = scope.ServiceProvider.GetRequiredService<IJobRunRepository>();
            await runs.CompleteAsync(id, completion, CancellationToken.None);

            JobHistoryOptions history = options.Value;
            await runs.DeleteHistoryAsync(
                jobId,
                completion.FinishedAt - history.Retention,
                history.MaxRunsPerJob,
                CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Cannot record the end of run {RunId} of job {JobId}", id, jobId);
        }
        finally
        {
            tracker.Release(jobId, id);
        }
    }

    private static Guid? ReadRunId(JobDataMap data)
    {
        return data.TryGetValue(JobDataKeys.RunId, out object? value) && Guid.TryParse(value?.ToString(), out Guid id)
            ? id
            : null;
    }

    private static JobRunTrigger ReadTrigger(JobDataMap data)
    {
        return data.TryGetValue(JobDataKeys.Trigger, out object? value)
            && Enum.TryParse(value?.ToString(), out JobRunTrigger trigger)
            ? trigger
            : JobRunTrigger.Schedule;
    }
}
