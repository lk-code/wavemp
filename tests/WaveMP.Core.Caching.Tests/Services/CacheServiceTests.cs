using AwesomeAssertions;
using NSubstitute;
using WaveMP.Core.Caching.Covers;
using WaveMP.Core.Caching.Services;
using WaveMP.Core.Caching.Stores;

namespace WaveMP.Core.Caching.Tests.Services;

[TestFixture]
public class CacheServiceTests
{
    [Test]
    public void GetUsage_Always_ReturnsTheUsageOfTheStore()
    {
        ICacheStore store = Substitute.For<ICacheStore>();
        CacheUsage usage = new(true, 10, 1, 100, new CacheAreaUsage(10, 1), CacheAreaUsage.Empty);
        store.GetUsage().Returns(usage);

        new CacheService(store, Substitute.For<ICoverImageCache>()).GetUsage().Should().Be(usage);
    }

    [Test]
    public async Task ClearAsync_Always_ClearsTheStoreAndForgetsTheFailures()
    {
        ICacheStore store = Substitute.For<ICacheStore>();
        ICoverImageCache covers = Substitute.For<ICoverImageCache>();

        await new CacheService(store, covers).ClearAsync();

        Received.InOrder(() =>
        {
            store.ClearAsync(Arg.Any<CancellationToken>());
            covers.ForgetFailures();
        });
    }
}
