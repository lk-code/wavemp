using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Streaming.Signatures;

/// <summary>
/// Holds the key that signs the media URLs, separate from the key of the session tokens. The key is created on the
/// first start and kept in the database, so URLs stay valid after a restart and no further setting is needed.
/// </summary>
public sealed class MediaUrlKeyStore(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<MediaUrlKeyStore> logger)
{
    /// <summary>
    /// Name of the secret in the database.
    /// </summary>
    public const string SecretName = "media-url-signing-key";

    /// <summary>
    /// Size of the key in bytes (512 bits, the block size of HMAC-SHA256).
    /// </summary>
    public const int KeySize = 64;

    /// <summary>
    /// The signing key. Available after <see cref="InitializeAsync"/>.
    /// </summary>
    public byte[] Key
    {
        get => field ?? throw new InvalidOperationException("The media URL signing key is not loaded.");
        private set;
    }

    /// <summary>
    /// Loads the key from the database or creates it. Runs on start, after the migrations.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        IServerSecretRepository secrets = scope.ServiceProvider.GetRequiredService<IServerSecretRepository>();

        byte[]? value = await secrets.FindAsync(SecretName, cancellationToken);
        if (value is null)
        {
            byte[] created = RandomNumberGenerator.GetBytes(KeySize);
            if (await secrets.TryAddAsync(SecretName, created, timeProvider.GetUtcNow(), cancellationToken))
            {
                logger.LogInformation("Created the key that signs the media URLs");
                value = created;
            }
            else
            {
                // Another instance created the key at the same time.
                value = await secrets.FindAsync(SecretName, cancellationToken)
                    ?? throw new InvalidOperationException("The media URL signing key could not be loaded.");
            }
        }

        Key = value;
    }
}
