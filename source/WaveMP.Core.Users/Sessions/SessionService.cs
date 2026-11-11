using Microsoft.Extensions.Options;
using WaveMP.Data.Entities;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Users.Sessions;

internal sealed class SessionService(
    IUserRepository users,
    ISessionRepository sessions,
    TimeProvider timeProvider,
    IOptions<SessionOptions> options) : ISessionService
{
    public async Task<ActiveSession?> CreateAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        User? user = await users.FindAsync(profileId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        // Expired sessions are removed here instead of in a background job.
        DateTimeOffset now = timeProvider.GetUtcNow();
        await sessions.DeleteExpiredAsync(now, cancellationToken);

        Session session = new()
        {
            Id = Guid.CreateVersion7(now),
            UserId = user.Id,
            CreatedAt = now,
            ExpiresAt = now + options.Value.Lifetime,
        };
        await sessions.AddAsync(session, cancellationToken);

        return new ActiveSession(session, user);
    }

    public async Task<ActiveSession?> GetActiveAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        Session? session = await sessions.FindActiveAsync(sessionId, timeProvider.GetUtcNow(), cancellationToken);
        return session?.User is { } user ? new ActiveSession(session, user) : null;
    }

    public Task DeleteAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return sessions.DeleteAsync(sessionId, cancellationToken);
    }
}
