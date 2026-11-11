using WaveMP.Data.Projections;

namespace WaveMP.Core.Caching.Covers;

/// <summary>
/// Scaled covers as WebP in the cache, created when they are asked for or prepared in advance. Parallel requests for
/// the same picture convert it only once. If a picture cannot be converted (ffmpeg is missing, the picture is damaged,
/// the cache folder cannot be written), the picture is delivered as stored, and the failure is remembered for
/// <see cref="Options.CacheOptions.FailureRetryDelay"/>.
/// </summary>
public interface ICoverImageCache
{
    /// <summary>
    /// The first cover of the ordered list that can be delivered: without <paramref name="size"/> as stored, otherwise
    /// scaled to <see cref="CoverSizes.Snap"/> of the size. <c>null</c> if none can be read.
    /// </summary>
    Task<CoverDelivery?> DeliverAsync(
        IReadOnlyList<CoverCandidateRow> ordered,
        int? size,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates the missing sizes of the first readable cover of the ordered list, without marking existing files as
    /// used. Stops without creating anything once the cache is nearly full.
    /// </summary>
    Task<CoverPrepareOutcome> PrepareAsync(
        IReadOnlyList<CoverCandidateRow> ordered,
        IReadOnlyList<int> sizes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Forgets the remembered failures, so the next request tries again, e.g. after the cache was cleared.
    /// </summary>
    void ForgetFailures();
}
