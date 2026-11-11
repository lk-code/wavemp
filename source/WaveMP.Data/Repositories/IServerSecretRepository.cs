namespace WaveMP.Data.Repositories;

/// <summary>
/// Access to the secrets of the server.
/// </summary>
public interface IServerSecretRepository
{
    Task<byte[]?> FindAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a secret. Returns <c>false</c> if a secret with this name already exists, e.g. because another instance
    /// created it at the same time.
    /// </summary>
    Task<bool> TryAddAsync(
        string name,
        byte[] value,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken = default);
}
