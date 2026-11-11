using Microsoft.Extensions.Logging;
using Quartz;
using WaveMP.Core.Jobs.Definitions;

namespace WaveMP.Core.Jobs.Schedulers;

internal sealed class JobScheduler(
    ISchedulerFactory schedulerFactory,
    IEnumerable<JobRegistration> registrations,
    IEnumerable<IJobScheduleProvider> scheduleProviders,
    TimeProvider timeProvider,
    ILogger<JobScheduler> logger) : IJobScheduler
{
    /// <summary>
    /// How late a scheduled run may start before it counts as missed and is skipped. Quartz' default of one minute
    /// would start a scheduled run right after a manual or file watcher run that held the job for less than that.
    /// </summary>
    internal static readonly TimeSpan ScheduledMisfireThreshold = TimeSpan.FromSeconds(1);

    private readonly JobRegistration[] _registrations = [.. registrations];
    private readonly IJobScheduleProvider[] _scheduleProviders = [.. scheduleProviders];

    public async Task RegisterAndScheduleAllAsync(CancellationToken cancellationToken = default)
    {
        IScheduler scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        foreach (JobRegistration registration in _registrations)
        {
            IJobDetail job = JobBuilder.Create()
                .OfType(registration.JobType)
                .WithIdentity(JobKeyOf(registration.JobId))
                .StoreDurably(true)
                .Build();
            await scheduler.AddJob(job, AddJobOptions.Replacing, cancellationToken);
        }

        foreach (IJobScheduleProvider provider in _scheduleProviders)
        {
            await ScheduleAsync(scheduler, provider, JobScheduleReason.Startup, cancellationToken);
        }
    }

    public async Task RescheduleAsync(string jobId, CancellationToken cancellationToken = default)
    {
        if (FindScheduleProvider(jobId) is not { } provider)
        {
            return;
        }

        IScheduler scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        await ScheduleAsync(scheduler, provider, JobScheduleReason.SettingsChanged, cancellationToken);
    }

    public async Task<DateTimeOffset?> GetNextRunAsync(string jobId, CancellationToken cancellationToken = default)
    {
        IScheduler scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        ITrigger? trigger = await scheduler.GetTrigger(TriggerKeyOf(jobId), cancellationToken);
        return trigger?.NextFireTimeUtc;
    }

    public async Task TriggerAsync(
        string jobId,
        IReadOnlyDictionary<string, string> data,
        CancellationToken cancellationToken = default)
    {
        IScheduler scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        JobDataMap map = [];
        foreach ((string key, string value) in data)
        {
            map[key] = value;
        }

        await scheduler.TriggerJob(JobKeyOf(jobId), map, cancellationToken);
    }

    public IJobScheduleProvider? FindScheduleProvider(string jobId)
    {
        return _scheduleProviders.FirstOrDefault(provider => provider.JobId == jobId);
    }

    internal static JobKey JobKeyOf(string jobId)
    {
        return new JobKey(jobId);
    }

    internal static TriggerKey TriggerKeyOf(string jobId)
    {
        return new TriggerKey(jobId + ".schedule");
    }

    private async Task ScheduleAsync(
        IScheduler scheduler,
        IJobScheduleProvider provider,
        JobScheduleReason reason,
        CancellationToken cancellationToken)
    {
        TriggerKey key = TriggerKeyOf(provider.JobId);
        if (provider.GetSchedule(reason) is not { } schedule)
        {
            await scheduler.UnscheduleJob(key, cancellationToken);
            return;
        }

        // A run that was missed, e.g. while a manual run of the same job was running, is skipped instead of being
        // made up right after: once the job is free again more than the threshold after the scheduled time, the
        // trigger moves on to its next scheduled time.
        ITrigger trigger = TriggerBuilder.Create(timeProvider)
            .WithIdentity(key)
            .ForJob(JobKeyOf(provider.JobId))
            .StartAt(timeProvider.GetUtcNow() + schedule.FirstRunDelay)
            .WithMisfireThreshold(ScheduledMisfireThreshold)
            .WithSchedule(SimpleScheduleBuilder.Create()
                .WithInterval(schedule.Interval)
                .RepeatForever()
                .WithMisfireInstruction(SimpleTriggerMisfireInstruction.NextWithRemainingCount))
            .Build();

        await scheduler.ScheduleJob(trigger, ScheduleJobOptions.Replacing, cancellationToken);
        logger.LogInformation(
            "Scheduled job {JobId} every {Interval}, next run at {NextRun}",
            provider.JobId,
            schedule.Interval,
            trigger.StartTimeUtc);
    }
}
