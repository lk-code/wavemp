using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WaveMP.Core.Streaming.Signatures;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Streaming.Tests.Signatures;

[TestFixture]
public class MediaUrlKeyStoreTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 11, 12, 0, 0, TimeSpan.Zero);

    private IServerSecretRepository _secrets = null!;
    private ServiceProvider _services = null!;
    private MediaUrlKeyStore _store = null!;

    [SetUp]
    public void SetUp()
    {
        _secrets = Substitute.For<IServerSecretRepository>();
        // NSubstitute would return an empty array; no key is stored unless a test says so.
        _secrets.FindAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((byte[]?)null);
        _services = new ServiceCollection().AddScoped(_ => _secrets).BuildServiceProvider();
        TimeProvider timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(_now);
        _store = new MediaUrlKeyStore(
            _services.GetRequiredService<IServiceScopeFactory>(),
            timeProvider,
            NullLogger<MediaUrlKeyStore>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        _services.Dispose();
    }

    [Test]
    public void Key_BeforeInitialization_Throws()
    {
        Func<byte[]> act = () => _store.Key;

        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public async Task InitializeAsync_WithoutStoredKey_CreatesAndStoresARandomKeyOfItsOwn()
    {
        byte[]? stored = null;
        _secrets.TryAddAsync(
                "media-url-signing-key",
                Arg.Do<byte[]>(value => stored = value),
                _now,
                Arg.Any<CancellationToken>())
            .Returns(true);

        await _store.InitializeAsync();

        stored.Should().NotBeNull().And.HaveCount(MediaUrlKeyStore.KeySize);
        stored.Should().NotBeEquivalentTo(new byte[MediaUrlKeyStore.KeySize]);
        _store.Key.Should().Equal(stored);
    }

    [Test]
    public async Task InitializeAsync_WithStoredKey_UsesIt()
    {
        byte[] key = TestKeys.Create(7);
        _secrets.FindAsync(MediaUrlKeyStore.SecretName, Arg.Any<CancellationToken>()).Returns(key);

        await _store.InitializeAsync();

        _store.Key.Should().Equal(key);
        await _secrets.DidNotReceive().TryAddAsync(
            Arg.Any<string>(),
            Arg.Any<byte[]>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task InitializeAsync_KeyCreatedByAnotherInstanceAtTheSameTime_UsesTheStoredKey()
    {
        byte[] other = TestKeys.Create(9);
        _secrets.FindAsync(MediaUrlKeyStore.SecretName, Arg.Any<CancellationToken>())
            .Returns((byte[]?)null, other);
        _secrets.TryAddAsync(
                MediaUrlKeyStore.SecretName,
                Arg.Any<byte[]>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>())
            .Returns(false);

        await _store.InitializeAsync();

        _store.Key.Should().Equal(other);
    }
}
