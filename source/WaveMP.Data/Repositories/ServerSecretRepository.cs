using Microsoft.EntityFrameworkCore;
using Npgsql;
using WaveMP.Data.Entities;

namespace WaveMP.Data.Repositories;

internal sealed class ServerSecretRepository(WaveMpDbContext dbContext) : IServerSecretRepository
{
    public Task<byte[]?> FindAsync(string name, CancellationToken cancellationToken = default)
    {
        return dbContext.ServerSecrets.AsNoTracking()
            .Where(secret => secret.Name == name)
            .Select(secret => secret.Value)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> TryAddAsync(
        string name,
        byte[] value,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken = default)
    {
        ServerSecret secret = new() { Name = name, Value = value, CreatedAt = createdAt };
        dbContext.ServerSecrets.Add(secret);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
        })
        {
            dbContext.Entry(secret).State = EntityState.Detached;
            return false;
        }
    }
}
