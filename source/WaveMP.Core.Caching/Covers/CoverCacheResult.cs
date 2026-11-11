namespace WaveMP.Core.Caching.Covers;

/// <summary>
/// Result of preparing the covers, stored as JSON with the run of the job.
/// </summary>
/// <param name="RemovedStale">Scaled pictures removed because their picture changed or no longer exists.</param>
/// <param name="Created">Covers whose missing sizes were created.</param>
/// <param name="Existing">Covers that the cache already had in all sizes.</param>
/// <param name="WithoutSource">Lists of covers of which none could be read.</param>
/// <param name="Failed">Covers that could not be converted.</param>
/// <param name="StoppedAtLimit">The preparation stopped because the cache is nearly full.</param>
/// <param name="Evicted">Files removed afterwards because the cache was larger than its limit.</param>
/// <param name="SizeBytes">Size of the cache afterwards in bytes.</param>
public sealed record CoverCacheResult(
    int RemovedStale,
    int Created,
    int Existing,
    int WithoutSource,
    int Failed,
    bool StoppedAtLimit,
    int Evicted,
    long SizeBytes);
