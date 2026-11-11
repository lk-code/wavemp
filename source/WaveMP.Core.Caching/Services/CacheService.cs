using WaveMP.Core.Caching.Covers;
using WaveMP.Core.Caching.Stores;

namespace WaveMP.Core.Caching.Services;

internal sealed class CacheService(ICacheStore store, ICoverImageCache covers) : ICacheService
{
    public CacheUsage GetUsage()
    {
        return store.GetUsage();
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await store.ClearAsync(cancellationToken);
        covers.ForgetFailures();
    }
}
