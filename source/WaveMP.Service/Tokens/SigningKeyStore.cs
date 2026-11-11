using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using WaveMP.Data.Repositories;

namespace WaveMP.Service.Tokens;

/// <summary>
/// Holds the key that signs the session tokens. The key is created on the first start and kept in the database, so
/// tokens stay valid after a restart and no further setting is needed.
/// </summary>
public sealed class SigningKeyStore(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<SigningKeyStore> logger)
{
    /// <summary>
    /// Size of the key in bytes (512 bits, the block size of HMAC-SHA256).
    /// </summary>
    public const int KeySize = 64;

    private SymmetricSecurityKey? _key;

    /// <summary>
    /// The signing key. Available after <see cref="InitializeAsync"/>.
    /// </summary>
    public SecurityKey Key => _key ?? throw new InvalidOperationException("The signing key is not loaded.");

    /// <summary>
    /// Loads the key from the database or creates it. Runs on start, after the migrations.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        IServerSecretRepository secrets = scope.ServiceProvider.GetRequiredService<IServerSecretRepository>();

        byte[]? value = await secrets.FindAsync(ServerSecretNames.JwtSigningKey, cancellationToken);
        if (value is null)
        {
            byte[] created = RandomNumberGenerator.GetBytes(KeySize);
            if (await secrets.TryAddAsync(
                ServerSecretNames.JwtSigningKey,
                created,
                timeProvider.GetUtcNow(),
                cancellationToken))
            {
                logger.LogInformation("Created the key that signs the session tokens");
                value = created;
            }
            else
            {
                // Another instance created the key at the same time.
                value = await secrets.FindAsync(ServerSecretNames.JwtSigningKey, cancellationToken)
                    ?? throw new InvalidOperationException("The signing key could not be loaded.");
            }
        }

        _key = new SymmetricSecurityKey(value);
    }
}
