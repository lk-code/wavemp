using AwesomeAssertions;
using WaveMP.Core.Jobs.Runs;

namespace WaveMP.Core.Jobs.Tests.Runs;

[TestFixture]
public class JobRunTrackerTests
{
    private const string JobId = "test-job";

    private JobRunTracker _tracker = null!;

    [SetUp]
    public void SetUp()
    {
        _tracker = new JobRunTracker();
    }

    [Test]
    public void GetState_WithoutRuns_IsIdle()
    {
        _tracker.GetState(JobId).Should().Be(JobActivityState.Idle);
        _tracker.GetActiveRun(JobId).Should().BeNull();
    }

    [Test]
    public void TryReserve_WhenIdle_QueuesTheRun()
    {
        var runId = Guid.NewGuid();

        bool reserved = _tracker.TryReserve(JobId, runId);

        reserved.Should().BeTrue();
        _tracker.GetActiveRun(JobId).Should().Be(new JobRunActivity(runId, JobActivityState.Queued));
    }

    [Test]
    public void TryReserve_WhenQueuedOrRunning_IsRejected()
    {
        var first = Guid.NewGuid();
        _tracker.TryReserve(JobId, first);

        bool whileQueued = _tracker.TryReserve(JobId, Guid.NewGuid());
        _tracker.MarkRunning(JobId, first, "fire-1");
        bool whileRunning = _tracker.TryReserve(JobId, Guid.NewGuid());

        whileQueued.Should().BeFalse();
        whileRunning.Should().BeFalse();
        _tracker.TryReserve("other-job", Guid.NewGuid()).Should().BeTrue();
    }

    [Test]
    public void MarkRunning_WithQueuedRun_MakesItTheRunningOneOfTheFiring()
    {
        var runId = Guid.NewGuid();
        _tracker.TryReserve(JobId, runId);

        _tracker.MarkRunning(JobId, runId, "fire-1");

        _tracker.GetActiveRun(JobId).Should().Be(new JobRunActivity(runId, JobActivityState.Running));
        _tracker.FindRunId("fire-1").Should().Be(runId);
        _tracker.FindRunId("fire-2").Should().BeNull();
    }

    [Test]
    public void MarkRunning_WhileAnotherRunIsQueued_PrefersTheRunningOne()
    {
        var queued = Guid.NewGuid();
        var scheduled = Guid.NewGuid();
        _tracker.TryReserve(JobId, queued);

        _tracker.MarkRunning(JobId, scheduled, "fire-1");
        JobRunActivity? whileRunning = _tracker.GetActiveRun(JobId);
        _tracker.Release(JobId, scheduled);

        whileRunning.Should().Be(new JobRunActivity(scheduled, JobActivityState.Running));
        _tracker.GetActiveRun(JobId).Should().Be(new JobRunActivity(queued, JobActivityState.Queued));
    }

    [Test]
    public void Release_WithOtherRunId_KeepsTheRunningRun()
    {
        var runId = Guid.NewGuid();
        _tracker.MarkRunning(JobId, runId, "fire-1");

        _tracker.Release(JobId, Guid.NewGuid());

        _tracker.GetState(JobId).Should().Be(JobActivityState.Running);
    }

    [Test]
    public void Release_WithRunningRun_MakesTheJobIdleAgain()
    {
        var runId = Guid.NewGuid();
        _tracker.TryReserve(JobId, runId);
        _tracker.MarkRunning(JobId, runId, "fire-1");

        _tracker.Release(JobId, runId);

        _tracker.GetState(JobId).Should().Be(JobActivityState.Idle);
        _tracker.FindRunId("fire-1").Should().BeNull();
        _tracker.TryReserve(JobId, Guid.NewGuid()).Should().BeTrue();
    }

    [Test]
    public void TryReserve_FromManyThreads_ReservesExactlyOnce()
    {
        int reserved = 0;

        Parallel.For(0, 100, _ =>
        {
            if (_tracker.TryReserve(JobId, Guid.NewGuid()))
            {
                Interlocked.Increment(ref reserved);
            }
        });

        reserved.Should().Be(1);
    }
}
