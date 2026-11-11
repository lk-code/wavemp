using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WaveMP.Core.Caching.Options;
using WaveMP.Core.Caching.Stores;
using WaveMP.Core.Jobs.Runs;
using WaveMP.Core.Library.Covers;
using WaveMP.Data.Entities;
using WaveMP.Data.Projections;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Caching.Covers;

internal sealed class CoverCachePreparer(
    ICacheStore store,
    ICoverRepository repository,
    ICoverService covers,
    ICoverImageCache cache,
    IOptions<CacheOptions> options,
    ILogger<CoverCachePreparer> logger) : ICoverCachePreparer
{
    /// <summary>
    /// Lists of covers prepared in parallel before the next lists are read.
    /// </summary>
    internal const int BatchSize = 50;

    public async Task<CoverCacheResult> RunAsync(CancellationToken cancellationToken = default)
    {
        if (!store.IsAvailable)
        {
            throw new JobFailedException(
                JobErrorCode.CacheFolderUnwritable,
                $"The cache folder {store.RootPath} cannot be written.");
        }

        int removedStale = await RemoveStaleAsync(cancellationToken);

        Counters counters = new();
        List<IReadOnlyList<CoverCandidateRow>> batch = [];
        await foreach (IReadOnlyList<CoverCandidateRow> ordered in covers.EnumerateShownCoversAsync(cancellationToken))
        {
            batch.Add(ordered);
            if (batch.Count < BatchSize)
            {
                continue;
            }

            await PrepareAsync(batch, counters, cancellationToken);
            batch.Clear();
            if (counters.StoppedAtLimit)
            {
                break;
            }
        }

        if (batch.Count > 0 && !counters.StoppedAtLimit)
        {
            await PrepareAsync(batch, counters, cancellationToken);
        }

        int evicted = await store.TrimAsync(cancellationToken);
        CoverCacheResult result = new(
            removedStale,
            counters.Created,
            counters.Existing,
            counters.WithoutSource,
            counters.Failed,
            counters.StoppedAtLimit,
            evicted,
            store.GetUsage().SizeBytes);
        logger.LogInformation(
            "Covers prepared: {Created} created, {Existing} existing, {Failed} failed, {RemovedStale} stale removed, "
            + "{Evicted} removed by the clean-up, stopped at the limit: {StoppedAtLimit}",
            result.Created,
            result.Existing,
            result.Failed,
            result.RemovedStale,
            result.Evicted,
            result.StoppedAtLimit);
        return result;
    }

    /// <summary>
    /// Removes the scaled pictures whose picture changed (its name then carries an older stamp) or no longer exists.
    /// </summary>
    private async Task<int> RemoveStaleAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<CoverSourceRow> sources = await repository.ListCoverSourcesAsync(cancellationToken);
        HashSet<string> current = new(sources.Select(CoverCacheKey.PrefixOf), StringComparer.Ordinal);
        return await store.RemoveAsync(
            CacheArea.Covers,
            name => !CoverCacheKey.TryReadSourcePrefix(name, out string prefix) || !current.Contains(prefix),
            cancellationToken);
    }

    private async Task PrepareAsync(
        IReadOnlyList<IReadOnlyList<CoverCandidateRow>> batch,
        Counters counters,
        CancellationToken cancellationToken)
    {
        ParallelOptions parallel = new()
        {
            MaxDegreeOfParallelism = Math.Max(1, options.Value.MaxParallelEncodes),
            CancellationToken = cancellationToken,
        };
        await Parallel.ForEachAsync(batch, parallel, async (ordered, token) =>
        {
            if (counters.StoppedAtLimit || counters.EncoderUnavailable || counters.StoreUnavailable)
            {
                return;
            }

            CoverPrepareOutcome outcome = await cache.PrepareAsync(ordered, CoverSizes.Prepared, token);
            counters.Count(outcome);
        });

        if (counters.EncoderUnavailable)
        {
            throw new JobFailedException(JobErrorCode.FfmpegUnavailable, "ffmpeg cannot be started.");
        }

        if (counters.StoreUnavailable)
        {
            throw new JobFailedException(
                JobErrorCode.CacheFolderUnwritable,
                $"The cache folder {store.RootPath} cannot be written.");
        }
    }

    private sealed class Counters
    {
        private int _created;
        private int _existing;
        private int _withoutSource;
        private int _failed;
        private int _stoppedAtLimit;
        private int _encoderUnavailable;
        private int _storeUnavailable;

        public int Created => Volatile.Read(ref _created);

        public int Existing => Volatile.Read(ref _existing);

        public int WithoutSource => Volatile.Read(ref _withoutSource);

        public int Failed => Volatile.Read(ref _failed);

        public bool StoppedAtLimit => Volatile.Read(ref _stoppedAtLimit) != 0;

        public bool EncoderUnavailable => Volatile.Read(ref _encoderUnavailable) != 0;

        public bool StoreUnavailable => Volatile.Read(ref _storeUnavailable) != 0;

        public void Count(CoverPrepareOutcome outcome)
        {
            switch (outcome)
            {
                case CoverPrepareOutcome.Created:
                    Interlocked.Increment(ref _created);
                    break;
                case CoverPrepareOutcome.Existing:
                    Interlocked.Increment(ref _existing);
                    break;
                case CoverPrepareOutcome.NoReadableSource:
                    Interlocked.Increment(ref _withoutSource);
                    break;
                case CoverPrepareOutcome.Failed:
                    Interlocked.Increment(ref _failed);
                    break;
                case CoverPrepareOutcome.LimitReached:
                    Volatile.Write(ref _stoppedAtLimit, 1);
                    break;
                case CoverPrepareOutcome.EncoderUnavailable:
                    Volatile.Write(ref _encoderUnavailable, 1);
                    break;
                case CoverPrepareOutcome.StoreUnavailable:
                    Volatile.Write(ref _storeUnavailable, 1);
                    break;
            }
        }
    }
}
