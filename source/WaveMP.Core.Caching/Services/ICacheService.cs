using WaveMP.Core.Caching.Stores;

namespace WaveMP.Core.Caching.Services;

/// <summary>
/// The cache for the administration: how much space it uses, and clearing it.
/// </summary>
public interface ICacheService
{
    CacheUsage GetUsage();

    /// <summary>
    /// Removes all files of the cache and forgets the remembered failures. Covers are created again the next time
    /// they are shown or after the next scan.
    /// </summary>
    Task ClearAsync(CancellationToken cancellationToken = default);
}
