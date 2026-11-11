using Microsoft.Extensions.DependencyInjection;
using WaveMP.Core.Jobs.Runs;
using WaveMP.Core.Jobs.Schedulers;

namespace WaveMP.Core.Jobs.Extensions;

public static class ServiceProviderExtensions
{
    /// <summary>
    /// Marks runs that the last shutdown interrupted, then adds all jobs to the scheduler and creates their schedules
    /// from code and settings. Runs on start, after the settings were loaded; the jobs start to run once the
    /// application has started.
    /// </summary>
    public static async Task InitializeWaveMpJobsAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using (AsyncServiceScope scope = services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<JobRunRecovery>().RecoverAsync(cancellationToken);
        }

        await services.GetRequiredService<IJobScheduler>().RegisterAndScheduleAllAsync(cancellationToken);
    }
}
