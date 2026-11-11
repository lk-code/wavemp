using Microsoft.Extensions.DependencyInjection;
using WaveMP.Core.Caching.Stores;

namespace WaveMP.Core.Caching.Extensions;

public static class ServiceProviderExtensions
{
    /// <summary>
    /// Creates the cache folder, removes unfinished files and loads what the cache holds. Runs on start, after the
    /// settings were loaded. A cache folder that cannot be written does not stop the start: covers are then delivered
    /// unscaled, and the reason is logged.
    /// </summary>
    public static Task InitializeWaveMpCacheAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        return services.GetRequiredService<ICacheStore>().InitializeAsync(cancellationToken);
    }
}
