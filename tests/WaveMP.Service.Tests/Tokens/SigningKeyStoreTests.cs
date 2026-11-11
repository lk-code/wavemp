using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using WaveMP.Data.Repositories;
using WaveMP.Service.Tokens;

namespace WaveMP.Service.Tests.Tokens;

[TestFixture]
public class SigningKeyStoreTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 11, 12, 0, 0, TimeSpan.Zero);

    private IServerSecretRepository _secrets = null!;
    private ServiceProvider _services = null!;
    private SigningKeyStore _store = null!;

    [SetUp]
    public void SetUp()
    {
        _secrets = Substitute.For<IServerSecretRepository>();
        // NSubstitute would return an empty array; no key is stored unless a test says so.
        _secrets.FindAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((byte[]?)null);
        _services = new ServiceCollection().AddScoped(_ => _secrets).BuildServiceProvider();
        TimeProvider timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(_now);
        _store = new SigningKeyStore(
            _services.GetRequiredService<IServiceScopeFactory>(),
            timeProvider,
            NullLogger<SigningKeyStore>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        _services.Dispose();
    }

    [Test]
    public void Key_BeforeInitialization_Throws()
    {
        Func<SecurityKey> act = () => _store.Key;

        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public async Task InitializeAsync_WithoutStoredKey_CreatesAndStoresRandomKey()
    {
        byte[]? stored = null;
        _secrets.TryAddAsync(
                ServerSecretNames.JwtSigningKey,
                Arg.Do<byte[]>(value => stored = value),
                _now,
                Arg.Any<CancellationToken>())
            .Returns(true);

        await _store.InitializeAsync();

        stored.Should().NotBeNull().And.HaveCount(SigningKeyStore.KeySize);
        stored.Should().NotBeEquivalentTo(new byte[SigningKeyStore.KeySize]);
        _store.Key.Should().BeOfType<SymmetricSecurityKey>().Which.Key.Should().Equal(stored);
    }

    [Test]
    public async Task InitializeAsync_WithStoredKey_UsesIt()
    {
        byte[] key = [.. Enumerable.Range(0, SigningKeyStore.KeySize).Select(index => (byte)index)];
        _secrets.FindAsync(ServerSecretNames.JwtSigningKey, Arg.Any<CancellationToken>()).Returns(key);

        await _store.InitializeAsync();

        _store.Key.Should().BeOfType<SymmetricSecurityKey>().Which.Key.Should().Equal(key);
        await _secrets.DidNotReceive().TryAddAsync(
            Arg.Any<string>(),
            Arg.Any<byte[]>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task InitializeAsync_WhenAnotherInstanceStoredAKeyFirst_UsesThatKey()
    {
        byte[] key = [.. Enumerable.Repeat((byte)7, SigningKeyStore.KeySize)];
        _secrets.FindAsync(ServerSecretNames.JwtSigningKey, Arg.Any<CancellationToken>()).Returns(null, key);
        _secrets.TryAddAsync(
                Arg.Any<string>(),
                Arg.Any<byte[]>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>())
            .Returns(false);

        await _store.InitializeAsync();

        _store.Key.Should().BeOfType<SymmetricSecurityKey>().Which.Key.Should().Equal(key);
    }
}
