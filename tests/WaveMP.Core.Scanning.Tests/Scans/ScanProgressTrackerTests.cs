using AwesomeAssertions;
using WaveMP.Core.Scanning.Scans;

namespace WaveMP.Core.Scanning.Tests.Scans;

[TestFixture]
public class ScanProgressTrackerTests
{
    [Test]
    public void Progress_OfARun_FollowsItsPhasesAndCountsUntilItCompletes()
    {
        ScanProgressTracker tracker = new();
        var runId = Guid.NewGuid();

        tracker.Start(runId);
        tracker.Discovered(3, 10);
        ScanProgress? discovering = tracker.Get(runId);
        tracker.Planned(2, 4);
        tracker.FileRead();
        tracker.FileRead();
        tracker.DirectoryProcessed();
        ScanProgress? processing = tracker.Get(runId);
        tracker.CleaningUp();
        ScanProgress? cleaningUp = tracker.Get(runId);
        tracker.Complete(runId);

        discovering.Should().Be(new ScanProgress(ScanPhase.Discovering, 3, 10, 0, 0, 0, 0));
        processing.Should().Be(new ScanProgress(ScanPhase.Processing, 3, 10, 2, 1, 4, 2));
        cleaningUp!.Phase.Should().Be(ScanPhase.CleaningUp);
        tracker.Get(runId).Should().BeNull();
    }

    [Test]
    public void Get_OtherRun_ReturnsNull()
    {
        ScanProgressTracker tracker = new();
        tracker.Start(Guid.NewGuid());

        tracker.Get(Guid.NewGuid()).Should().BeNull();
    }
}
