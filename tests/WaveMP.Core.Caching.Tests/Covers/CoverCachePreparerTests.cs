using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WaveMP.Core.Caching.Covers;
using WaveMP.Core.Caching.Options;
using WaveMP.Core.Caching.Stores;
using WaveMP.Core.Jobs.Runs;
using WaveMP.Core.Library.Covers;
using WaveMP.Data.Entities;
using WaveMP.Data.Projections;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Caching.Tests.Covers;

[TestFixture]
public class CoverCachePreparerTests
{
    private static readonly DateTimeOffset _modifiedAt = new(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);

    private ICacheStore _store = null!;
    private ICoverRepository _repository = null!;
    private ICoverService _covers = null!;
    private ICoverImageCache _cache = null!;
    private CoverCachePreparer _preparer = null!;

    [SetUp]
    public void SetUp()
    {
        _store = Substitute.For<ICacheStore>();
        _store.IsAvailable.Returns(true);
        _store.RootPath.Returns("/data/cache");
        _store.GetUsage().Returns(new CacheUsage(
            true,
            1234,
            3,
            2_000_000_000,
            new CacheAreaUsage(1234, 3),
            CacheAreaUsage.Empty));
        _repository = Substitute.For<ICoverRepository>();
        _repository.ListCoverSourcesAsync(Arg.Any<CancellationToken>()).Returns([]);
        _covers = Substitute.For<ICoverService>();
        _covers.EnumerateShownCoversAsync(Arg.Any<CancellationToken>()).Returns(_ => ListsAsync([]));
        _cache = Substitute.For<ICoverImageCache>();
        _preparer = new CoverCachePreparer(
            _store,
            _repository,
            _covers,
            _cache,
            Microsoft.Extensions.Options.Options.Create(new CacheOptions { MaxParallelEncodes = 2 }),
            NullLogger<CoverCachePreparer>.Instance);
    }

    [Test]
    public async Task RunAsync_CacheFolderNotWritable_FailsWithItsCode()
    {
        _store.IsAvailable.Returns(false);

        Func<Task> run = () => _preparer.RunAsync();

        (await run.Should().ThrowAsync<JobFailedException>())
            .Which.Code.Should().Be(JobErrorCode.CacheFolderUnwritable);
    }

    [Test]
    public async Task RunAsync_Always_RemovesThePicturesOfChangedAndRemovedCovers()
    {
        CoverSourceRow current = new(Guid.CreateVersion7(), CoverSource.File, 100, _modifiedAt);
        _repository.ListCoverSourcesAsync(Arg.Any<CancellationToken>()).Returns([current]);
        Func<string, bool>? stale = null;
        _store.RemoveAsync(CacheArea.Covers, Arg.Any<Func<string, bool>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                stale = call.ArgAt<Func<string, bool>>(1);
                return 4;
            });

        CoverCacheResult result = await _preparer.RunAsync();

        string prefix = CoverCacheKey.PrefixOf(current);
        string changed = CoverCacheKey.PrefixOf(current with { ModifiedAt = _modifiedAt.AddHours(1) });
        result.RemovedStale.Should().Be(4);
        stale!(CoverCacheKey.FileNameOf(prefix, 96)).Should().BeFalse();
        stale(CoverCacheKey.FileNameOf(changed, 96)).Should().BeTrue();
        stale("ab/unknown.webp").Should().BeTrue();
    }

    [Test]
    public async Task RunAsync_ShownCovers_PreparesTheSmallerSizesAndCountsTheOutcomes()
    {
        IReadOnlyList<CoverCandidateRow>[] lists = [[Candidate()], [Candidate()], [Candidate()], [Candidate()]];
        _covers.EnumerateShownCoversAsync(Arg.Any<CancellationToken>()).Returns(_ => ListsAsync(lists));
        _cache.PrepareAsync(lists[0], Arg.Any<IReadOnlyList<int>>(), Arg.Any<CancellationToken>())
            .Returns(CoverPrepareOutcome.Created);
        _cache.PrepareAsync(lists[1], Arg.Any<IReadOnlyList<int>>(), Arg.Any<CancellationToken>())
            .Returns(CoverPrepareOutcome.Existing);
        _cache.PrepareAsync(lists[2], Arg.Any<IReadOnlyList<int>>(), Arg.Any<CancellationToken>())
            .Returns(CoverPrepareOutcome.NoReadableSource);
        _cache.PrepareAsync(lists[3], Arg.Any<IReadOnlyList<int>>(), Arg.Any<CancellationToken>())
            .Returns(CoverPrepareOutcome.Failed);
        _store.TrimAsync(Arg.Any<CancellationToken>()).Returns(2);

        CoverCacheResult result = await _preparer.RunAsync();

        result.Should().Be(new CoverCacheResult(0, 1, 1, 1, 1, false, 2, 1234));
        await _cache.Received(4).PrepareAsync(
            Arg.Any<IReadOnlyList<CoverCandidateRow>>(),
            Arg.Is<IReadOnlyList<int>>(sizes => sizes.SequenceEqual(new[] { 96, 300 })),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RunAsync_CacheNearlyFull_StopsPreparing()
    {
        IReadOnlyList<CoverCandidateRow>[] lists =
            [.. Enumerable.Range(0, CoverCachePreparer.BatchSize * 3).Select(_ => new[] { Candidate() })];
        _covers.EnumerateShownCoversAsync(Arg.Any<CancellationToken>()).Returns(_ => ListsAsync(lists));
        _cache.PrepareAsync(
                Arg.Any<IReadOnlyList<CoverCandidateRow>>(),
                Arg.Any<IReadOnlyList<int>>(),
                Arg.Any<CancellationToken>())
            .Returns(CoverPrepareOutcome.LimitReached);

        CoverCacheResult result = await _preparer.RunAsync();

        result.StoppedAtLimit.Should().BeTrue();
        _cache.ReceivedCalls().Count().Should().BeLessThanOrEqualTo(CoverCachePreparer.BatchSize);
        await _store.Received(1).TrimAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RunAsync_FfmpegMissing_FailsWithItsCode()
    {
        _covers.EnumerateShownCoversAsync(Arg.Any<CancellationToken>()).Returns(_ => ListsAsync([[Candidate()]]));
        _cache.PrepareAsync(
                Arg.Any<IReadOnlyList<CoverCandidateRow>>(),
                Arg.Any<IReadOnlyList<int>>(),
                Arg.Any<CancellationToken>())
            .Returns(CoverPrepareOutcome.EncoderUnavailable);

        Func<Task> run = () => _preparer.RunAsync();

        (await run.Should().ThrowAsync<JobFailedException>())
            .Which.Code.Should().Be(JobErrorCode.FfmpegUnavailable);
    }

    private static async IAsyncEnumerable<IReadOnlyList<CoverCandidateRow>> ListsAsync(
        IEnumerable<IReadOnlyList<CoverCandidateRow>> lists)
    {
        foreach (IReadOnlyList<CoverCandidateRow> list in lists)
        {
            await Task.Yield();
            yield return list;
        }
    }

    private static CoverCandidateRow Candidate()
    {
        return new CoverCandidateRow(
            Guid.NewGuid(),
            Guid.CreateVersion7(),
            Guid.NewGuid(),
            null,
            null,
            null,
            null,
            null,
            true,
            CoverSource.File,
            "Album/cover.jpg",
            "image/jpeg",
            100,
            _modifiedAt);
    }
}
