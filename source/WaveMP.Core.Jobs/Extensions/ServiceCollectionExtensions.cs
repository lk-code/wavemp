using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Quartz;
using WaveMP.Core.Jobs.Definitions;
using WaveMP.Core.Jobs.Listeners;
using WaveMP.Core.Jobs.Options;
using WaveMP.Core.Jobs.Runs;
using WaveMP.Core.Jobs.Schedulers;
using WaveMP.Core.Jobs.Services;

namespace WaveMP.Core.Jobs.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the background jobs: Quartz.NET with the in-memory store only (no Quartz tables), started by a hosted
    /// service once the application has started, the run history and the job overview. Schedules are created on start
    /// by <see cref="ServiceProviderExtensions.InitializeWaveMpJobsAsync"/>. Needs the repositories of
    /// <c>AddWaveMpData</c>.
    /// </summary>
    public static IServiceCollection AddWaveMpJobs(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddOptions<JobHistoryOptions>().BindConfiguration(JobHistoryOptions.SectionName);

        services.AddSingleton<IJobRunTracker, JobRunTracker>();
        services.AddSingleton<IJobScheduler, JobScheduler>();
        services.AddSingleton<JobRunListener>();
        services.AddScoped<IJobService, JobService>();
        services.AddScoped<JobRunRecovery>();

        services.AddQuartz(quartz =>
        {
            quartz.UseInMemoryStore(_ => { });
            quartz.AddJobListener<JobRunListener>(
                provider => provider.GetRequiredService<JobRunListener>(),
                [Matchers.AllJobs()]);
        });
        services.AddQuartzHostedService(options =>
        {
            options.AwaitApplicationStarted = true;
            options.WaitForJobsToComplete = true;
        });

        return services;
    }

    /// <summary>
    /// Registers a job under its id. Every run of it is built in a dependency injection scope of its own; jobs must
    /// carry <see cref="DisallowConcurrentExecutionAttribute"/>, so they never run in parallel to themselves.
    /// </summary>
    public static IServiceCollection AddWaveMpJob<TJob>(this IServiceCollection services, string jobId)
        where TJob : class, IJob
    {
        services.AddSingleton(new JobRegistration(jobId, typeof(TJob)));
        return services;
    }
}
