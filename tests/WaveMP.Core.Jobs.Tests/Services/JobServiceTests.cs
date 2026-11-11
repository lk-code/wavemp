using AwesomeAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using WaveMP.Core.Jobs.Definitions;
using WaveMP.Core.Jobs.Runs;
using WaveMP.Core.Jobs.Schedulers;
using WaveMP.Core.Jobs.Services;
using WaveMP.Data.Entities;
using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Jobs.Tests.Services;

[TestFixture]
public class JobServiceTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 11, 10, 0, 0, TimeSpan.Zero);

    private JobRunTracker _tracker = null!;
    private IJobScheduler _scheduler = null!;
    private IJobRunRepository _runs = null!;
    private JobService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _tracker = new JobRunTracker();
        _scheduler = Substitute.For<IJobScheduler>();
        _runs = Substitute.For<IJobRunRepository>();
        _runs.FindLatestAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, JobRun>());
        TimeProvider timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(_now);
        _service = new JobService(
            [new JobRegistration("b-job", typeof(object)), new JobRegistration("a-job", typeof(object))],
            _tracker,
            _scheduler,
            _runs,
            timeProvider);
    }

    [Test]
    public async Task StartRunAsync_WithUnknownJob_ReturnsNotFound()
    {
        JobRunStartResult result = await _service.StartRunAsync(new JobRunRequest("unknown", JobRunTrigger.Manual));

        result.Error.Should().Be(JobRunStartError.NotFound);
        await _scheduler.DidNotReceive().TriggerAsync(
            Arg.Any<string>(),
            Arg.Any<IReadOnlyDictionary<string, string>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task StartRunAsync_WhenIdle_QueuesARunAndTriggersTheJobWithItsData()
    {
        JobRunStartResult result = await _service.StartRunAsync(
            new JobRunRequest("a-job", JobRunTrigger.FileWatcher, "[\"Artist\"]"));

        result.IsSuccess.Should().BeTrue();
        JobRun run = result.Run!;
        run.Status.Should().Be(JobRunStatus.Queued);
        run.Trigger.Should().Be(JobRunTrigger.FileWatcher);
        run.QueuedAt.Should().Be(_now);
        run.Id.Version.Should().Be(7);
        await _runs.Received(1).AddAsync(run, Arg.Any<CancellationToken>());
        await _scheduler.Received(1).TriggerAsync(
            "a-job",
            Arg.Is<IReadOnlyDictionary<string, string>>(data =>
                data[JobDataKeys.RunId] == run.Id.ToString()
                && data[JobDataKeys.Trigger] == "FileWatcher"
                && data[JobDataKeys.Payload] == "[\"Artist\"]"),
            Arg.Any<CancellationToken>());
        _tracker.GetActiveRun("a-job").Should().Be(new JobRunActivity(run.Id, JobActivityState.Queued));
    }

    [Test]
    public async Task StartRunAsync_WhileActive_ReturnsAlreadyActive()
    {
        await _service.StartRunAsync(new JobRunRequest("a-job", JobRunTrigger.Manual));

        JobRunStartResult second = await _service.StartRunAsync(new JobRunRequest("a-job", JobRunTrigger.Manual));

        second.Error.Should().Be(JobRunStartError.AlreadyActive);
        await _runs.Received(1).AddAsync(Arg.Any<JobRun>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task StartRunAsync_WhenTriggeringFails_ReleasesTheJobAndMarksTheRunAsFailed()
    {
        _scheduler.TriggerAsync(
                Arg.Any<string>(),
                Arg.Any<IReadOnlyDictionary<string, string>>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Scheduler stopped"));

        Func<Task> act = () => _service.StartRunAsync(new JobRunRequest("a-job", JobRunTrigger.Manual));

        await act.Should().ThrowAsync<InvalidOperationException>();
        _tracker.GetState("a-job").Should().Be(JobActivityState.Idle);
        await _runs.Received(1).CompleteAsync(
            Arg.Any<Guid>(),
            Arg.Is<JobRunCompletion>(completion => completion.Status == JobRunStatus.Failed
                && completion.ErrorCode == JobErrorCode.Failed),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ListAsync_Always_ListsTheJobsByIdWithStateScheduleAndRuns()
    {
        IJobScheduleProvider provider = Substitute.For<IJobScheduleProvider>();
        provider.GetSchedule(JobScheduleReason.SettingsChanged)
            .Returns(new JobSchedule(TimeSpan.FromHours(6), TimeSpan.FromHours(6)));
        _scheduler.FindScheduleProvider("a-job").Returns(provider);
        _scheduler.GetNextRunAsync("a-job", Arg.Any<CancellationToken>()).Returns(_now.AddHours(1));
        JobRun last = new()
        {
            Id = Guid.NewGuid(),
            JobId = "a-job",
            Trigger = JobRunTrigger.Schedule,
            Status = JobRunStatus.Succeeded,
            QueuedAt = _now.AddHours(-5),
        };
        _runs.FindLatestAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, JobRun> { ["a-job"] = last });
        _tracker.MarkRunning("b-job", Guid.NewGuid(), "fire");

        Page<JobOverview> page = await _service.ListAsync(0, 10);

        page.Total.Should().Be(2);
        page.Items.Should().Equal(
            new JobOverview(
                "a-job",
                JobActivityState.Idle,
                new JobSchedule(TimeSpan.FromHours(6), TimeSpan.FromHours(6)),
                _now.AddHours(1),
                last),
            new JobOverview("b-job", JobActivityState.Running, null, null, null));
    }

    [Test]
    public async Task ListAsync_WithPaging_ReturnsOnlyThePage()
    {
        Page<JobOverview> page = await _service.ListAsync(1, 1);

        page.Items.Select(job => job.JobId).Should().Equal("b-job");
        page.Total.Should().Be(2);
    }

    [Test]
    public async Task GetRunAsync_WithRunOfOtherJob_ReturnsNull()
    {
        JobRun run = new()
        {
            Id = Guid.NewGuid(),
            JobId = "b-job",
            Trigger = JobRunTrigger.Manual,
            Status = JobRunStatus.Succeeded,
            QueuedAt = _now,
        };
        _runs.FindAsync(run.Id, Arg.Any<CancellationToken>()).Returns(run);

        (await _service.GetRunAsync("a-job", run.Id)).Should().BeNull();
        (await _service.GetRunAsync("b-job", run.Id)).Should().BeSameAs(run);
    }
}
