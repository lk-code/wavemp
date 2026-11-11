using WaveMP.Core.Caching.Stores;
using WaveMP.Core.Settings.Groups;
using WaveMP.Core.Settings.Handlers;

namespace WaveMP.Core.Caching.Jobs;

/// <summary>
/// Cleans up the cache in the background as soon as its limit is lowered.
/// </summary>
internal sealed class CacheLimitChangeHandler(ICacheStore store) : ISettingsChangeHandler<CacheSettings>
{
    public ValueTask OnSettingsChangedAsync(
        CacheSettings previous,
        CacheSettings current,
        CancellationToken cancellationToken)
    {
        if (current.MaxSizeMegabytes < previous.MaxSizeMegabytes)
        {
            store.ScheduleTrim();
        }

        return ValueTask.CompletedTask;
    }
}
