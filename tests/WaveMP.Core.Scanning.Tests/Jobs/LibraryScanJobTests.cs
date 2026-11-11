using System.Reflection;
using AwesomeAssertions;
using NSubstitute;
using Quartz;
using WaveMP.Core.Jobs.Definitions;
using WaveMP.Core.Jobs.Runs;
using WaveMP.Core.Jobs.Schedulers;
using WaveMP.Core.Scanning.Jobs;
using WaveMP.Core.Scanning.Options;
using WaveMP.Core.Scanning.Plans;
using WaveMP.Core.Scanning.Scans;
using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Settings.Snapshots;

namespace WaveMP.Core.Scanning.Tests.Jobs;

[TestFixture]
public class LibraryScanJobTests
{
    [Test]
    public void LibraryScanJob_Always_DisallowsConcurrentExecution()
    {
        typeof(LibraryScanJob).GetCustomAttribute<DisallowConcurrentExecutionAttribute>().Should().NotBeNull();
    }

    [Test]
    public async Task Execute_WithPayload_ScansTheFoldersForTheRunAndSetsTheResult()
    {
        ILibraryScanner scanner = Substitute.For<ILibraryScanner>();
        JobRunTracker tracker = new();
        var runId = Guid.NewGuid();
        tracker.MarkRunning(JobIds.LibraryScan, runId, "fire-1");
        ScanResult result = new(new ScanStatistics { TracksTotal = 3 }, []);
        scanner.ScanAsync(Arg.Any<ScanScope>(), runId, Arg.Any<CancellationToken>()).Returns(result);
        IJobExecutionContext context = Substitute.For<IJobExecutionContext>();
        context.FireInstanceId.Returns("fire-1");
        context.MergedJobDataMap.Returns(new JobDataMap
        {
            [JobDataKeys.Payload] = LibraryScanJob.CreatePayload(ScanScope.Of(["Artist/Album", "Other"])),
        });

        await new LibraryScanJob(scanner, tracker).Execute(context, CancellationToken.None);

        await scanner.Received(1).ScanAsync(
            Arg.Is<ScanScope>(scope => scope.Directories.SequenceEqual(new[] { "Artist/Album", "Other" })),
            runId,
            Arg.Any<CancellationToken>());
        context.Received().Result = result;
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("not json")]
    [TestCase("[]")]
    public void ReadScope_WithoutUsablePayload_IsAFullScan(string? payload)
    {
        JobDataMap data = payload is null ? [] : new JobDataMap { [JobDataKeys.Payload] = payload };

        LibraryScanJob.ReadScope(data).IsFull.Should().BeTrue();
    }

    [Test]
    public void ScheduleProvider_OnStartAndAfterChanges_UsesTheIntervalOfTheSettings()
    {
        ISettingsSnapshot<LibrarySettings> settings = Substitute.For<ISettingsSnapshot<LibrarySettings>>();
        settings.Current.Returns(new LibrarySettings { ScanIntervalMinutes = 30 });
        LibraryScanScheduleProvider provider = new(
            settings,
            Microsoft.Extensions.Options.Options.Create(new ScanOptions { StartupDelay = TimeSpan.FromMinutes(2) }));

        provider.JobId.Should().Be(JobIds.LibraryScan);
        provider.GetSchedule(JobScheduleReason.Startup)
            .Should().Be(new JobSchedule(TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(2)));
        provider.GetSchedule(JobScheduleReason.SettingsChanged)
            .Should().Be(new JobSchedule(TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30)));
    }

    [Test]
    public async Task Rescheduler_ChangedInterval_PlansTheScanAgain()
    {
        IJobScheduler scheduler = Substitute.For<IJobScheduler>();
        LibraryScanRescheduler rescheduler = new(scheduler);

        await rescheduler.OnSettingsChangedAsync(
            new LibrarySettings { ScanIntervalMinutes = 60 },
            new LibrarySettings { ScanIntervalMinutes = 30 },
            CancellationToken.None);

        await scheduler.Received(1).RescheduleAsync(JobIds.LibraryScan, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Rescheduler_OtherChanges_KeepTheSchedule()
    {
        IJobScheduler scheduler = Substitute.For<IJobScheduler>();
        LibraryScanRescheduler rescheduler = new(scheduler);

        await rescheduler.OnSettingsChangedAsync(
            new LibrarySettings { FileWatcherEnabled = true },
            new LibrarySettings { FileWatcherEnabled = false },
            CancellationToken.None);

        await scheduler.DidNotReceive().RescheduleAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
