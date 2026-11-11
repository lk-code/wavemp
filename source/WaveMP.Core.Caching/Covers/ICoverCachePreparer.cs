namespace WaveMP.Core.Caching.Covers;

/// <summary>
/// Brings the covers of the cache up to date with the library, e.g. after a scan.
/// </summary>
public interface ICoverCachePreparer
{
    /// <summary>
    /// Removes the scaled pictures of pictures that changed or no longer exist, creates the prepared sizes
    /// (<see cref="CoverSizes.Prepared"/>) of every cover the library shows until the cache is nearly full, and cleans
    /// up the cache if it is larger than its limit. Fails with a
    /// <see cref="WaveMP.Core.Jobs.Runs.JobFailedException"/> if the cache folder cannot be written or ffmpeg cannot be
    /// started.
    /// </summary>
    Task<CoverCacheResult> RunAsync(CancellationToken cancellationToken = default);
}
