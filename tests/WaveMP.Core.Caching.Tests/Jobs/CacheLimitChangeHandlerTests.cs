using NSubstitute;
using WaveMP.Core.Caching.Jobs;
using WaveMP.Core.Caching.Stores;
using WaveMP.Core.Settings.Groups;

namespace WaveMP.Core.Caching.Tests.Jobs;

[TestFixture]
public class CacheLimitChangeHandlerTests
{
    [TestCase(2000, 1000, true)]
    [TestCase(1000, 2000, false)]
    [TestCase(1000, 1000, false)]
    public async Task OnSettingsChangedAsync_LowerLimit_CleansUpTheCache(int previous, int current, bool trims)
    {
        ICacheStore store = Substitute.For<ICacheStore>();
        CacheLimitChangeHandler handler = new(store);

        await handler.OnSettingsChangedAsync(
            new CacheSettings { MaxSizeMegabytes = previous },
            new CacheSettings { MaxSizeMegabytes = current },
            CancellationToken.None);

        store.Received(trims ? 1 : 0).ScheduleTrim();
    }
}
