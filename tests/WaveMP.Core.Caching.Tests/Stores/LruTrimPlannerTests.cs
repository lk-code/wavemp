using AwesomeAssertions;
using WaveMP.Core.Caching.Stores;

namespace WaveMP.Core.Caching.Tests.Stores;

[TestFixture]
public class LruTrimPlannerTests
{
    private static readonly DateTimeOffset _start = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public void Plan_WithinTheLimit_RemovesNothing()
    {
        CacheIndexEntry[] entries = [Entry("a", 400, 1), Entry("b", 600, 2)];

        LruTrimPlanner.Plan(entries, 1000, 1000, 0.9).Should().BeEmpty();
    }

    [Test]
    public void Plan_OverTheLimit_RemovesTheLeastRecentlyUsedUntilTheTargetShare()
    {
        CacheIndexEntry newest = Entry("newest", 300, 5);
        CacheIndexEntry oldest = Entry("oldest", 300, 1);
        CacheIndexEntry middle = Entry("middle", 300, 3);
        CacheIndexEntry older = Entry("older", 300, 2);

        // 1200 bytes with a limit of 1000: down to at most 900.
        IReadOnlyList<CacheIndexEntry> planned = LruTrimPlanner.Plan([newest, oldest, middle, older], 1200, 1000, 0.9);

        planned.Should().Equal(oldest);
    }

    [Test]
    public void Plan_SameLastUse_OrdersByAreaAndName()
    {
        CacheIndexEntry b = Entry("b", 500, 1);
        CacheIndexEntry a = Entry("a", 500, 1);
        CacheIndexEntry transcode = Entry("0", 500, 1) with { Area = CacheArea.Transcodes };

        LruTrimPlanner.Plan([transcode, b, a], 1500, 1000, 0.5).Should().Equal(a, b);
    }

    [Test]
    public void Plan_LargeFiles_RemovesUntilTheTargetIsReached()
    {
        CacheIndexEntry[] entries = [Entry("a", 100, 1), Entry("b", 100, 2), Entry("c", 900, 3)];

        LruTrimPlanner.Plan(entries, 1100, 1000, 0.9).Select(entry => entry.Name).Should().Equal("a", "b");
    }

    private static CacheIndexEntry Entry(string name, long size, int minutes)
    {
        DateTimeOffset usedAt = _start.AddMinutes(minutes);
        return new CacheIndexEntry(CacheArea.Covers, name, size, usedAt, usedAt);
    }
}
