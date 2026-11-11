using AwesomeAssertions;
using WaveMP.Core.Scanning.Plans;
using WaveMP.Core.Scanning.Watchers;

namespace WaveMP.Core.Scanning.Tests.Watchers;

[TestFixture]
public class MediaChangeBatchTests
{
    private static readonly DateTimeOffset _start = new(2026, 11, 11, 10, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan _debounce = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan _maxDelay = TimeSpan.FromSeconds(30);

    [Test]
    public void IsDue_EmptyBatch_IsNever()
    {
        MediaChangeBatch batch = new();

        batch.IsDue(_start.AddHours(1), _debounce, _maxDelay).Should().BeFalse();
    }

    [Test]
    public void IsDue_AfterTheDebounceWithoutFurtherChanges_IsDue()
    {
        MediaChangeBatch batch = new();
        batch.Add("Artist/Album", _start);
        batch.Add("Artist/Album", _start.AddSeconds(3));

        batch.IsDue(_start.AddSeconds(7), _debounce, _maxDelay).Should().BeFalse();
        batch.IsDue(_start.AddSeconds(8), _debounce, _maxDelay).Should().BeTrue();
    }

    [Test]
    public void IsDue_WithChangesComingIn_IsDueAfterTheMaximumDelay()
    {
        MediaChangeBatch batch = new();
        for (int second = 0; second <= 30; second += 2)
        {
            batch.Add("Busy", _start.AddSeconds(second));
        }

        batch.IsDue(_start.AddSeconds(29), _debounce, _maxDelay).Should().BeFalse();
        batch.IsDue(_start.AddSeconds(30), _debounce, _maxDelay).Should().BeTrue();
    }

    [Test]
    public void Take_ChangedFolders_ReducesThemToAScopeAndEmptiesTheBatch()
    {
        MediaChangeBatch batch = new();
        batch.Add("Artist/Album", _start);
        batch.Add("Artist", _start);
        batch.Add("Other\\Folder", _start);

        ScanScope scope = batch.Take();

        scope.Directories.Should().Equal("Artist", "Other/Folder");
        batch.IsEmpty.Should().BeTrue();
        batch.FirstChange.Should().BeNull();
    }

    [Test]
    public void Take_AfterAFullScanRequest_IsAFullScan()
    {
        MediaChangeBatch batch = new();
        batch.Add("Artist", _start);
        batch.RequestFull(_start);

        batch.Take().IsFull.Should().BeTrue();
    }

    [Test]
    public void Restore_ScopeThatCouldNotStart_KeepsItsFirstChange()
    {
        MediaChangeBatch batch = new();
        batch.Add("Artist", _start);
        ScanScope scope = batch.Take();
        batch.Add("Other", _start.AddSeconds(10));

        batch.Restore(scope, _start);

        batch.FirstChange.Should().Be(_start);
        batch.Take().Directories.Should().Equal("Artist", "Other");
    }
}
