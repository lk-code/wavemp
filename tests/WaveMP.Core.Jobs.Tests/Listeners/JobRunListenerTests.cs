using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Quartz;
using WaveMP.Core.Jobs.Definitions;
using WaveMP.Core.Jobs.Listeners;
using WaveMP.Core.Jobs.Options;
using WaveMP.Core.Jobs.Runs;
using WaveMP.Data.Entities;
using WaveMP.Data.Repositories;
using JobRunStatus = WaveMP.Data.Entities.JobRunStatus;

namespace WaveMP.Core.Jobs.Tests.Listeners;

[TestFixture]
public class JobRunListenerTests
{
    private const string JobId = "test-job";
    private const string FireInstanceId = "fire-1";
    private static readonly DateTimeOffset _now = new(2026, 11, 11, 10, 0, 0, TimeSpan.Zero);

    private IJobRunRepository _runs = null!;
    private JobRunTracker _tracker = null!;
    private ServiceProvider _services = null!;
    private JobRunListener _listener = null!;

    [SetUp]
    public void SetUp()
    {
        _runs = Substitute.For<IJobRunRepository>();
        _tracker = new JobRunTracker();
        _services = new ServiceCollection().AddSingleton(_runs).BuildServiceProvider();
        TimeProvider timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(_now);
        _listener = new JobRunListener(
            _services.GetRequiredService<IServiceScopeFactory>(),
            _tracker,
            timeProvider,
            Microsoft.Extensions.Options.Options.Create(new JobHistoryOptions
            {
                Retention = TimeSpan.FromDays(30),
                MaxRunsPerJob = 100,
            }),
            NullLogger<JobRunListener>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        _services.Dispose();
    }

    [Test]
    public void Classify_WithoutException_Succeeds()
    {
        JobRunCompletion completion = JobRunListener.Classify(null, canceled: false, result: null, _now);

        completion.Should().Be(new JobRunCompletion(JobRunStatus.Succeeded, _now));
    }

    [Test]
    public void Classify_WithResult_StoresItAsJsonWithEnumNames()
    {
        JobRunCompletion completion = JobRunListener.Classify(
            null,
            canceled: false,
            new { FilesAdded = 3, Phase = JobScheduleReason.Startup },
            _now);

        completion.Result.Should().Be("{\"filesAdded\":3,\"phase\":\"Startup\"}");
    }

    [Test]
    public void Classify_WithCancellation_IsCanceled()
    {
        JobExecutionException exception = new(new OperationCanceledException());

        JobRunListener.Classify(exception, canceled: false, null, _now).Status.Should().Be(JobRunStatus.Canceled);
        JobRunListener.Classify(null, canceled: true, null, _now).Status.Should().Be(JobRunStatus.Canceled);
    }

    [Test]
    public void Classify_WithJobFailedException_FailsWithItsCode()
    {
        JobExecutionException exception = new(
            new JobFailedException(JobErrorCode.MediaFolderNotFound, "The media folder does not exist."));

        JobRunCompletion completion = JobRunListener.Classify(exception, canceled: false, null, _now);

        completion.Status.Should().Be(JobRunStatus.Failed);
        completion.ErrorCode.Should().Be(JobErrorCode.MediaFolderNotFound);
        completion.ErrorDetail.Should()
            .Be("WaveMP.Core.Jobs.Runs.JobFailedException: The media folder does not exist.");
    }

    [Test]
    public void Classify_WithUnexpectedException_FailsWithTheGeneralCodeAndTheCause()
    {
        JobExecutionException exception = new(new InvalidOperationException(new string('x', 5000)));

        JobRunCompletion completion = JobRunListener.Classify(exception, canceled: false, null, _now);

        completion.Status.Should().Be(JobRunStatus.Failed);
        completion.ErrorCode.Should().Be(JobErrorCode.Failed);
        completion.ErrorDetail.Should().StartWith("System.InvalidOperationException: xxx");
        completion.ErrorDetail!.Length.Should().Be(4000);
    }

    [Test]
    public async Task JobToBeExecuted_WithQueuedRun_MarksItAsRunning()
    {
        var runId = Guid.NewGuid();
        _tracker.TryReserve(JobId, runId);
        _runs.MarkRunningAsync(runId, _now, Arg.Any<CancellationToken>()).Returns(true);
        IJobExecutionContext context = CreateContext(new JobDataMap
        {
            [JobDataKeys.RunId] = runId.ToString(),
            [JobDataKeys.Trigger] = nameof(JobRunTrigger.Manual),
        });

        await _listener.JobToBeExecuted(context, CancellationToken.None);

        _tracker.GetActiveRun(JobId).Should().Be(new JobRunActivity(runId, JobActivityState.Running));
        await _runs.DidNotReceive().AddAsync(Arg.Any<JobRun>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task JobToBeExecuted_WithScheduledFiring_AddsARunningRun()
    {
        IJobExecutionContext context = CreateContext([]);

        await _listener.JobToBeExecuted(context, CancellationToken.None);

        await _runs.Received(1).AddAsync(
            Arg.Is<JobRun>(run => run.JobId == JobId
                && run.Trigger == JobRunTrigger.Schedule
                && run.Status == JobRunStatus.Running
                && run.QueuedAt == _now
                && run.StartedAt == _now
                && run.Id.Version == 7),
            Arg.Any<CancellationToken>());
        _tracker.GetState(JobId).Should().Be(JobActivityState.Running);
    }

    [Test]
    public async Task JobWasExecuted_AfterRun_CompletesItCleansUpTheHistoryAndReleasesTheJob()
    {
        var runId = Guid.NewGuid();
        _tracker.MarkRunning(JobId, runId, FireInstanceId);
        IJobExecutionContext context = CreateContext([]);
        context.Result.Returns(new { TracksTotal = 2 });

        await _listener.JobWasExecuted(context, null, CancellationToken.None);

        await _runs.Received(1).CompleteAsync(
            runId,
            new JobRunCompletion(JobRunStatus.Succeeded, _now, Result: "{\"tracksTotal\":2}"),
            Arg.Any<CancellationToken>());
        await _runs.Received(1).DeleteHistoryAsync(JobId, _now.AddDays(-30), 100, Arg.Any<CancellationToken>());
        _tracker.GetState(JobId).Should().Be(JobActivityState.Idle);
    }

    [Test]
    public async Task JobWasExecuted_WhenTheHistoryFails_SwallowsTheErrorAndReleasesTheJob()
    {
        var runId = Guid.NewGuid();
        _tracker.MarkRunning(JobId, runId, FireInstanceId);
        _runs.CompleteAsync(Arg.Any<Guid>(), Arg.Any<JobRunCompletion>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Database down"));

        Func<Task> act = async () => await _listener.JobWasExecuted(CreateContext([]), null, CancellationToken.None);

        await act.Should().NotThrowAsync();
        _tracker.GetState(JobId).Should().Be(JobActivityState.Idle);
    }

    [Test]
    public async Task JobExecutionVetoed_WithRunningRun_CancelsIt()
    {
        var runId = Guid.NewGuid();
        _tracker.MarkRunning(JobId, runId, FireInstanceId);

        await _listener.JobExecutionVetoed(CreateContext([]), CancellationToken.None);

        await _runs.Received(1).CompleteAsync(
            runId,
            new JobRunCompletion(JobRunStatus.Canceled, _now),
            Arg.Any<CancellationToken>());
    }

    private static IJobExecutionContext CreateContext(JobDataMap data)
    {
        IJobExecutionContext context = Substitute.For<IJobExecutionContext>();
        IJobDetail job = Substitute.For<IJobDetail>();
        job.Key.Returns(new JobKey(JobId));
        context.JobDetail.Returns(job);
        context.MergedJobDataMap.Returns(data);
        context.FireInstanceId.Returns(FireInstanceId);
        context.Outcome.Returns(ExecutionOutcome.Succeeded);
        return context;
    }
}
