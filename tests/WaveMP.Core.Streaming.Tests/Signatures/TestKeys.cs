using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WaveMP.Core.Streaming.Signatures;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Streaming.Tests.Signatures;

/// <summary>
/// Fixed signing keys and key stores loaded with them.
/// </summary>
internal static class TestKeys
{
    public static byte[] Create(int seed)
    {
        return [.. Enumerable.Range(0, MediaUrlKeyStore.KeySize).Select(index => (byte)(index * seed))];
    }

    /// <summary>
    /// A key store that loaded <paramref name="key"/> from a substitute of the database.
    /// </summary>
    public static async Task<MediaUrlKeyStore> CreateStoreAsync(byte[] key)
    {
        IServerSecretRepository secrets = Substitute.For<IServerSecretRepository>();
        secrets.FindAsync(MediaUrlKeyStore.SecretName, Arg.Any<CancellationToken>()).Returns(key);
        await using ServiceProvider services = new ServiceCollection().AddScoped(_ => secrets).BuildServiceProvider();
        MediaUrlKeyStore store = new(
            services.GetRequiredService<IServiceScopeFactory>(),
            TimeProvider.System,
            NullLogger<MediaUrlKeyStore>.Instance);
        await store.InitializeAsync();
        return store;
    }
}
