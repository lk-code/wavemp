using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WaveMP.Core.Jobs.Definitions;
using WaveMP.Core.Jobs.Runs;
using WaveMP.Core.Jobs.Services;
using WaveMP.Core.Scanning.Scans;
using WaveMP.Data.Entities;
using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Scanning.Tests.Scans;

[TestFixture]
public class ScanServiceTests
{
    private IJobService _jobs = null!;
    private JobRunTracker _tracker = null!;
    private ScanProgressTracker _progress = null!;
    private ScanService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _jobs = Substitute.For<IJobService>();
        _tracker = new JobRunTracker();
        _progress = new ScanProgressTracker();
        _service = new ScanService(_jobs, _tracker, _progress, NullLogger<ScanService>.Instance);
    }

    [Test]
    public async Task StartAsync_Always_StartsAManualRunOfTheScanJob()
    {
        await _service.StartAsync();

        await _jobs.Received(1).StartRunAsync(
            new JobRunRequest(JobIds.LibraryScan, JobRunTrigger.Manual),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetCurrentAsync_WithoutActiveScan_ReturnsNull()
    {
        (await _service.GetCurrentAsync()).Should().BeNull();
    }

    [Test]
    public async Task GetCurrentAsync_WhileRunning_ReturnsTheRunWithItsProgress()
    {
        JobRun run = Run(JobRunStatus.Running);
        _tracker.MarkRunning(JobIds.LibraryScan, run.Id, "fire");
        _progress.Start(run.Id);
        _progress.Discovered(2, 5);
        _jobs.GetRunAsync(JobIds.LibraryScan, run.Id, Arg.Any<CancellationToken>()).Returns(run);

        ScanOverview? current = await _service.GetCurrentAsync();

        current!.Run.Should().BeSameAs(run);
        current.Progress.Should().Be(new ScanProgress(ScanPhase.Discovering, 2, 5, 0, 0, 0, 0));
        current.Result.Should().BeNull();
    }

    [Test]
    public async Task ListAsync_FinishedRuns_ReadTheirResults()
    {
        JobRun run = Run(
            JobRunStatus.Succeeded,
            """{"statistics":{"filesAdded":2,"tracksTotal":2,"isPartial":false},"issues":[{"path":"a.mp3","code":"ProbeFailed"}]}""");
        _jobs.ListRunsAsync(Arg.Any<JobRunListQuery>(), Arg.Any<CancellationToken>())
            .Returns(new Page<JobRun>([run, Run(JobRunStatus.Failed, "{broken")], 0, 10, 2));

        Page<ScanOverview> page = await _service.ListAsync(0, 10, SortDirection.Asc, JobRunStatus.Succeeded);

        page.Items[0].Result!.Statistics.FilesAdded.Should().Be(2);
        page.Items[0].Result!.Issues.Should().Equal(new ScanIssue("a.mp3", ScanIssueCode.ProbeFailed));
        page.Items[1].Result.Should().BeNull();
        await _jobs.Received(1).ListRunsAsync(
            new JobRunListQuery(JobIds.LibraryScan, 0, 10, SortDirection.Asc, JobRunStatus.Succeeded),
            Arg.Any<CancellationToken>());
    }

    private static JobRun Run(JobRunStatus status, string? result = null)
    {
        return new JobRun
        {
            Id = Guid.NewGuid(),
            JobId = JobIds.LibraryScan,
            Trigger = JobRunTrigger.Manual,
            Status = status,
            QueuedAt = DateTimeOffset.UtcNow,
            Result = result,
        };
    }
}
