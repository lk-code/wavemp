using System.Collections.Concurrent;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Quartz;
using WaveMP.Core.Jobs.Definitions;
using WaveMP.Core.Jobs.Extensions;
using WaveMP.Core.Jobs.Runs;
using WaveMP.Core.Jobs.Schedulers;
using WaveMP.Core.Jobs.Services;
using WaveMP.Data.Entities;
using WaveMP.Data.Repositories;
using JobRunStatus = WaveMP.Data.Entities.JobRunStatus;

namespace WaveMP.Core.Jobs.Tests.Hosts;

/// <summary>
/// Runs Quartz.NET in a real host, as in the service, with the run history in a substitute.
/// </summary>
[TestFixture]
public class QuartzHostTests
{
    private const string GatedJobId = "gated";
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(15);

    [Test]
    public async Task TriggeredTwice_WithDisallowConcurrentExecution_NeverRunsInParallel()
    {
        await using TestHost host = await TestHost.StartAsync();
        IJobScheduler scheduler = host.Services.GetRequiredService<IJobScheduler>();

        await scheduler.TriggerAsync(GatedJobId, new Dictionary<string, string>());
        await scheduler.TriggerAsync(GatedJobId, new Dictionary<string, string>());
        await host.Probe.WaitForStartsAsync(1);
        await Task.Delay(TimeSpan.FromMilliseconds(500));
        int startedWhileTheFirstRuns = host.Probe.Starts;
        host.Probe.Open();
        await host.Probe.WaitForEndsAsync(2);

        startedWhileTheFirstRuns.Should().Be(1);
        host.Probe.MaxConcurrency.Should().Be(1);
    }

    [Test]
    public async Task EveryRun_GetsADependencyInjectionScopeOfItsOwn()
    {
        await using TestHost host = await TestHost.StartAsync();
        host.Probe.Open();
        IJobScheduler scheduler = host.Services.GetRequiredService<IJobScheduler>();

        await scheduler.TriggerAsync(GatedJobId, new Dictionary<string, string>());
        await host.Probe.WaitForEndsAsync(1);
        await scheduler.TriggerAsync(GatedJobId, new Dictionary<string, string>());
        await host.Probe.WaitForEndsAsync(2);
        await host.Probe.WaitForDisposalsAsync(2);

        host.Probe.ScopedInstances.Distinct().Should().HaveCount(2);
    }

    [Test]
    public async Task StartRunAsync_WithJobService_RecordsTheRunFromQueuedToSucceeded()
    {
        await using TestHost host = await TestHost.StartAsync();
        host.Probe.Open();
        await using AsyncServiceScope scope = host.Services.CreateAsyncScope();
        IJobService jobs = scope.ServiceProvider.GetRequiredService<IJobService>();
        host.Runs.MarkRunningAsync(Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(true);

        JobRunStartResult result = await jobs.StartRunAsync(new JobRunRequest(GatedJobId, JobRunTrigger.Manual));
        await host.Probe.WaitForEndsAsync(1);
        await WaitUntilAsync(() => host.Services.GetRequiredService<IJobRunTracker>().GetState(GatedJobId)
            == JobActivityState.Idle);

        result.IsSuccess.Should().BeTrue();
        await host.Runs.Received(1).AddAsync(
            Arg.Is<JobRun>(run => run.Id == result.Run!.Id && run.Status == JobRunStatus.Queued),
            Arg.Any<CancellationToken>());
        await host.Runs.Received(1).MarkRunningAsync(
            result.Run!.Id,
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());
        await host.Runs.Received(1).CompleteAsync(
            result.Run.Id,
            Arg.Is<JobRunCompletion>(completion => completion.Status == JobRunStatus.Succeeded
                && completion.Result == "{\"runs\":1}"),
            Arg.Any<CancellationToken>());
        host.Probe.RunIds.Should().Equal(result.Run.Id);
    }

    [Test]
    public async Task Schedule_FromProvider_IsCreatedOnStartAndReplacedWhenTheSettingsChange()
    {
        TestScheduleProvider provider = new() { Interval = TimeSpan.FromHours(6) };
        await using TestHost host = await TestHost.StartAsync(provider);
        IJobScheduler scheduler = host.Services.GetRequiredService<IJobScheduler>();

        DateTimeOffset? first = await scheduler.GetNextRunAsync(GatedJobId);
        provider.Interval = TimeSpan.FromMinutes(30);
        await scheduler.RescheduleAsync(GatedJobId);
        DateTimeOffset? second = await scheduler.GetNextRunAsync(GatedJobId);

        first.Should().BeCloseTo(DateTimeOffset.UtcNow.AddHours(1), TimeSpan.FromMinutes(1));
        second.Should().BeCloseTo(DateTimeOffset.UtcNow.AddMinutes(30), TimeSpan.FromMinutes(1));
    }

    [Test]
    public async Task ScheduledRun_DueWhileAnotherRunHoldsTheJob_IsSkipped()
    {
        TestScheduleProvider provider = new() { Interval = TimeSpan.FromHours(1), StartupDelay = TimeSpan.FromSeconds(3) };
        await using TestHost host = await TestHost.StartAsync(provider);
        IJobScheduler scheduler = host.Services.GetRequiredService<IJobScheduler>();
        DateTimeOffset due = (await scheduler.GetNextRunAsync(GatedJobId))!.Value;

        await scheduler.TriggerAsync(GatedJobId, new Dictionary<string, string>());
        await host.Probe.WaitForStartsAsync(1);
        DateTimeOffset manualRunStarted = DateTimeOffset.UtcNow;
        await WaitUntilAsync(() => DateTimeOffset.UtcNow
            > due + JobScheduler.ScheduledMisfireThreshold + TimeSpan.FromMilliseconds(500));
        host.Probe.Open();
        await host.Probe.WaitForEndsAsync(1);
        await Task.Delay(TimeSpan.FromSeconds(1));

        manualRunStarted.Should().BeBefore(due, "the manual run must hold the job when the scheduled run is due");
        host.Probe.Starts.Should().Be(1);
        (await scheduler.GetNextRunAsync(GatedJobId)).Should().Be(due + provider.Interval);
    }

    [Test]
    public async Task TwoHosts_InOneProcess_RunTheirJobsIndependently()
    {
        await using TestHost first = await TestHost.StartAsync();
        await using TestHost second = await TestHost.StartAsync();
        first.Probe.Open();
        second.Probe.Open();

        await first.Services.GetRequiredService<IJobScheduler>()
            .TriggerAsync(GatedJobId, new Dictionary<string, string>());
        await second.Services.GetRequiredService<IJobScheduler>()
            .TriggerAsync(GatedJobId, new Dictionary<string, string>());

        await first.Probe.WaitForEndsAsync(1);
        await second.Probe.WaitForEndsAsync(1);
        first.Probe.Ends.Should().Be(1);
        second.Probe.Ends.Should().Be(1);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using CancellationTokenSource timeout = new(_timeout);
        while (!condition())
        {
            await Task.Delay(TimeSpan.FromMilliseconds(20), timeout.Token);
        }
    }

    /// <summary>
    /// Shared by the host and the job runs; counts runs and holds them until <see cref="Open"/>.
    /// </summary>
    internal sealed class JobProbe
    {
        private readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _running;
        private int _maxConcurrency;
        private int _starts;
        private int _ends;
        private int _disposals;

        public int Starts => Volatile.Read(ref _starts);

        public int Ends => Volatile.Read(ref _ends);

        public int MaxConcurrency => Volatile.Read(ref _maxConcurrency);

        public ConcurrentQueue<ScopedDependency> ScopedInstances { get; } = [];

        public ConcurrentQueue<Guid> RunIds { get; } = [];

        public void Open()
        {
            _gate.TrySetResult();
        }

        public async Task RunAsync(ScopedDependency dependency, Guid? runId, CancellationToken cancellationToken)
        {
            ScopedInstances.Enqueue(dependency);
            if (runId is { } id)
            {
                RunIds.Enqueue(id);
            }

            int running = Interlocked.Increment(ref _running);
            int max;
            while (running > (max = Volatile.Read(ref _maxConcurrency)))
            {
                Interlocked.CompareExchange(ref _maxConcurrency, running, max);
            }

            Interlocked.Increment(ref _starts);
            try
            {
                await _gate.Task.WaitAsync(cancellationToken);
            }
            finally
            {
                Interlocked.Decrement(ref _running);
                Interlocked.Increment(ref _ends);
            }
        }

        public void Disposed()
        {
            Interlocked.Increment(ref _disposals);
        }

        public Task WaitForStartsAsync(int count)
        {
            return WaitUntilAsync(() => Starts >= count);
        }

        public Task WaitForEndsAsync(int count)
        {
            return WaitUntilAsync(() => Ends >= count);
        }

        public Task WaitForDisposalsAsync(int count)
        {
            return WaitUntilAsync(() => Volatile.Read(ref _disposals) >= count);
        }
    }

    /// <summary>
    /// A scoped service: every run must get a new instance that is disposed after the run.
    /// </summary>
    internal sealed class ScopedDependency(JobProbe probe) : IDisposable
    {
        public void Dispose()
        {
            probe.Disposed();
        }
    }

    [DisallowConcurrentExecution]
    internal sealed class GatedJob(JobProbe probe, ScopedDependency dependency, IJobRunTracker tracker) : IJob
    {
        public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken)
        {
            await probe.RunAsync(dependency, tracker.FindRunId(context.FireInstanceId), cancellationToken);
            context.Result = new { Runs = 1 };
        }
    }

    internal sealed class TestScheduleProvider : IJobScheduleProvider
    {
        public TimeSpan Interval { get; set; }

        public TimeSpan StartupDelay { get; init; } = TimeSpan.FromHours(1);

        public string JobId => GatedJobId;

        public JobSchedule? GetSchedule(JobScheduleReason reason)
        {
            return new JobSchedule(Interval, reason == JobScheduleReason.Startup ? StartupDelay : Interval);
        }
    }

    private sealed class TestHost : IAsyncDisposable
    {
        private readonly IHost _host;

        private TestHost(IHost host, IJobRunRepository runs)
        {
            _host = host;
            Runs = runs;
        }

        public IServiceProvider Services => _host.Services;

        public JobProbe Probe => Services.GetRequiredService<JobProbe>();

        public IJobRunRepository Runs { get; }

        public static async Task<TestHost> StartAsync(IJobScheduleProvider? scheduleProvider = null)
        {
            IJobRunRepository runs = Substitute.For<IJobRunRepository>();
            HostApplicationBuilder builder = Host.CreateApplicationBuilder();
            builder.Logging.SetMinimumLevel(LogLevel.Warning);
            builder.Services.AddSingleton(runs);
            builder.Services.AddSingleton<JobProbe>();
            builder.Services.AddScoped<ScopedDependency>();
            builder.Services.AddWaveMpJobs();
            builder.Services.AddWaveMpJob<GatedJob>(GatedJobId);
            if (scheduleProvider is not null)
            {
                builder.Services.AddSingleton(scheduleProvider);
            }

            IHost host = builder.Build();

            // As in the service: the jobs are planned before the host starts.
            await host.Services.InitializeWaveMpJobsAsync();
            await host.StartAsync();
            return new TestHost(host, runs);
        }

        public async ValueTask DisposeAsync()
        {
            Probe.Open();
            await _host.StopAsync();
            _host.Dispose();
        }
    }
}
