using Microsoft.EntityFrameworkCore;
using WaveMP.Data.Entities;

namespace WaveMP.Data.Repositories;

internal sealed class SessionRepository(WaveMpDbContext dbContext) : ISessionRepository
{
    public async Task AddAsync(Session session, CancellationToken cancellationToken = default)
    {
        dbContext.Sessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<Session?> FindActiveAsync(
        Guid sessionId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Sessions.AsNoTracking()
            .Include(session => session.User)
            .FirstOrDefaultAsync(session => session.Id == sessionId && session.ExpiresAt > now, cancellationToken);
    }

    public Task DeleteAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return dbContext.Sessions
            .Where(session => session.Id == sessionId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public Task<int> DeleteExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        return dbContext.Sessions
            .Where(session => session.ExpiresAt <= now)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
