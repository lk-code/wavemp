using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WaveMP.Core.Caching.Images;
using WaveMP.Core.Caching.Options;
using WaveMP.Core.Caching.Stores;
using WaveMP.Core.Library.Covers;
using WaveMP.Data.Projections;

namespace WaveMP.Core.Caching.Covers;

internal sealed class CoverImageCache(
    ICacheStore store,
    ICoverImageEncoder encoder,
    ICoverSourceReader reader,
    IOptions<CacheOptions> options,
    TimeProvider timeProvider,
    ILogger<CoverImageCache> logger) : ICoverImageCache
{
    internal const string ContentType = "image/webp";
    internal const int MaxRememberedFailures = 10_000;

    // Conversions in progress by prefix and sizes; parallel requests wait for the same one.
    private readonly ConcurrentDictionary<string, Lazy<Task<CoverPrepareOutcome>>> _inProgress =
        new(StringComparer.Ordinal);

    // Pictures that could not be converted, until when they are not tried again.
    private readonly ConcurrentDictionary<string, DateTimeOffset> _failures = new(StringComparer.Ordinal);
    private long _encoderBlockedUntilTicks;

    public async Task<CoverDelivery?> DeliverAsync(
        IReadOnlyList<CoverCandidateRow> ordered,
        int? size,
        CancellationToken cancellationToken = default)
    {
        if (size is not { } requested)
        {
            foreach (CoverCandidateRow candidate in ordered)
            {
                if (await OpenOriginalAsync(candidate, cancellationToken) is { } original)
                {
                    return original;
                }
            }

            return null;
        }

        int scaled = CoverSizes.Snap(requested);
        foreach (CoverCandidateRow candidate in ordered)
        {
            string prefix = CoverCacheKey.PrefixOf(candidate);
            string name = CoverCacheKey.FileNameOf(prefix, scaled);
            if (TryOpenScaled(candidate, prefix, scaled) is { } cached)
            {
                return cached;
            }

            if (CanConvert(prefix))
            {
                CoverPrepareOutcome outcome = await CreateSharedAsync(candidate, prefix, [scaled], cancellationToken);
                if (outcome == CoverPrepareOutcome.NoReadableSource)
                {
                    continue;
                }

                if (outcome == CoverPrepareOutcome.Created && TryOpenScaled(candidate, prefix, scaled) is { } created)
                {
                    return created;
                }

                logger.LogDebug("Delivering {Name} unscaled: {Outcome}", name, outcome);
            }

            // The picture cannot be scaled: it is delivered as stored.
            if (await OpenOriginalAsync(candidate, cancellationToken) is { } original)
            {
                return original;
            }
        }

        return null;
    }

    public async Task<CoverPrepareOutcome> PrepareAsync(
        IReadOnlyList<CoverCandidateRow> ordered,
        IReadOnlyList<int> sizes,
        CancellationToken cancellationToken = default)
    {
        if (!store.IsAvailable)
        {
            return CoverPrepareOutcome.StoreUnavailable;
        }

        if (IsEncoderBlocked())
        {
            return CoverPrepareOutcome.EncoderUnavailable;
        }

        foreach (CoverCandidateRow candidate in ordered)
        {
            string prefix = CoverCacheKey.PrefixOf(candidate);
            int[] missing = [.. sizes
                .Distinct()
                .Where(size => !store.Contains(CacheArea.Covers, CoverCacheKey.FileNameOf(prefix, size)))];
            if (missing.Length == 0)
            {
                return CoverPrepareOutcome.Existing;
            }

            if (HasFailed(prefix))
            {
                return CoverPrepareOutcome.Failed;
            }

            if (IsNearLimit())
            {
                return CoverPrepareOutcome.LimitReached;
            }

            CoverPrepareOutcome outcome = await CreateSharedAsync(candidate, prefix, missing, cancellationToken);
            if (outcome != CoverPrepareOutcome.NoReadableSource)
            {
                return outcome;
            }
        }

        return CoverPrepareOutcome.NoReadableSource;
    }

    public void ForgetFailures()
    {
        _failures.Clear();
        Interlocked.Exchange(ref _encoderBlockedUntilTicks, 0);
    }

    private CoverDelivery? TryOpenScaled(CoverCandidateRow candidate, string prefix, int size)
    {
        Stream? content = store.TryOpen(CacheArea.Covers, CoverCacheKey.FileNameOf(prefix, size));
        return content is null
            ? null
            : new CoverDelivery(content, ContentType, CoverCacheKey.ETagOf(prefix, size), candidate.ModifiedAt);
    }

    private async Task<CoverDelivery?> OpenOriginalAsync(
        CoverCandidateRow candidate,
        CancellationToken cancellationToken)
    {
        CoverImage? image = await reader.OpenAsync(candidate, cancellationToken);
        return image is null
            ? null
            : new CoverDelivery(
                image.Content,
                image.ContentType,
                CoverCacheKey.OriginalETagOf(CoverCacheKey.PrefixOf(candidate)),
                candidate.ModifiedAt);
    }

    private bool CanConvert(string prefix)
    {
        return store.IsAvailable && !IsEncoderBlocked() && !HasFailed(prefix);
    }

    /// <summary>
    /// Runs the conversion once for all callers that ask for the same picture and sizes at the same time. The shared
    /// work is never canceled by a single caller; a caller that gives up only stops waiting.
    /// </summary>
    private Task<CoverPrepareOutcome> CreateSharedAsync(
        CoverCandidateRow candidate,
        string prefix,
        IReadOnlyList<int> sizes,
        CancellationToken cancellationToken)
    {
        string key = prefix + "|" + string.Join(',', sizes.Select(size => size.ToString(CultureInfo.InvariantCulture)));
        Lazy<Task<CoverPrepareOutcome>>? own = null;
        own = new Lazy<Task<CoverPrepareOutcome>>(() => RunSharedAsync(key, own!, candidate, prefix, sizes));
        Lazy<Task<CoverPrepareOutcome>> work = _inProgress.GetOrAdd(key, own);
        return work.Value.WaitAsync(cancellationToken);
    }

    private async Task<CoverPrepareOutcome> RunSharedAsync(
        string key,
        Lazy<Task<CoverPrepareOutcome>> self,
        CoverCandidateRow candidate,
        string prefix,
        IReadOnlyList<int> sizes)
    {
        try
        {
            return await CreateAsync(candidate, prefix, sizes);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Cannot create the scaled pictures of the cover {CoverId}", candidate.CoverId);
            RememberFailure(prefix);
            return CoverPrepareOutcome.Failed;
        }
        finally
        {
            _inProgress.TryRemove(new KeyValuePair<string, Lazy<Task<CoverPrepareOutcome>>>(key, self));
        }
    }

    private async Task<CoverPrepareOutcome> CreateAsync(
        CoverCandidateRow candidate,
        string prefix,
        IReadOnlyList<int> sizes)
    {
        if (!store.IsAvailable)
        {
            return CoverPrepareOutcome.StoreUnavailable;
        }

        CoverImage? image = await reader.OpenAsync(candidate, CancellationToken.None);
        if (image is null)
        {
            return CoverPrepareOutcome.NoReadableSource;
        }

        (string Name, CoverEncodeTarget Target)[] targets = [.. sizes.Select(size => (
            CoverCacheKey.FileNameOf(prefix, size),
            new CoverEncodeTarget(size, store.CreateTemporaryPath(".webp"))))];
        try
        {
            CoverEncodeStatus status;
            await using (image.Content)
            {
                status = await encoder.EncodeAsync(
                    image.Content,
                    image.ContentType,
                    [.. targets.Select(target => target.Target)],
                    CancellationToken.None);
            }

            switch (status)
            {
                case CoverEncodeStatus.Unavailable:
                    BlockEncoder();
                    return CoverPrepareOutcome.EncoderUnavailable;
                case CoverEncodeStatus.Failed:
                    logger.LogDebug("The cover {CoverId} cannot be converted", candidate.CoverId);
                    RememberFailure(prefix);
                    return CoverPrepareOutcome.Failed;
            }

            foreach ((string name, CoverEncodeTarget target) in targets)
            {
                if (!store.Commit(CacheArea.Covers, name, target.OutputPath))
                {
                    RememberFailure(prefix);
                    return CoverPrepareOutcome.Failed;
                }
            }

            return CoverPrepareOutcome.Created;
        }
        finally
        {
            foreach ((_, CoverEncodeTarget target) in targets)
            {
                store.DeleteTemporary(target.OutputPath);
            }
        }
    }

    private bool IsNearLimit()
    {
        CacheUsage usage = store.GetUsage();
        return usage.SizeBytes >= usage.MaxSizeBytes * options.Value.TrimTargetRatio;
    }

    private bool HasFailed(string prefix)
    {
        if (!_failures.TryGetValue(prefix, out DateTimeOffset until))
        {
            return false;
        }

        if (until > timeProvider.GetUtcNow())
        {
            return true;
        }

        _failures.TryRemove(new KeyValuePair<string, DateTimeOffset>(prefix, until));
        return false;
    }

    private void RememberFailure(string prefix)
    {
        if (_failures.Count >= MaxRememberedFailures)
        {
            _failures.Clear();
        }

        _failures[prefix] = timeProvider.GetUtcNow() + options.Value.FailureRetryDelay;
    }

    private bool IsEncoderBlocked()
    {
        return timeProvider.GetUtcNow().UtcTicks < Interlocked.Read(ref _encoderBlockedUntilTicks);
    }

    private void BlockEncoder()
    {
        DateTimeOffset until = timeProvider.GetUtcNow() + options.Value.FailureRetryDelay;
        Interlocked.Exchange(ref _encoderBlockedUntilTicks, until.UtcTicks);
    }
}
