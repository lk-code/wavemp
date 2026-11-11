using Microsoft.EntityFrameworkCore;
using WaveMP.Data.Entities;
using WaveMP.Data.Queries;

namespace WaveMP.Data.Repositories;

internal sealed class PlayHistoryRepository(WaveMpDbContext dbContext) : IPlayHistoryRepository
{
    public async Task AddAsync(PlayHistoryEntry entry, CancellationToken cancellationToken = default)
    {
        dbContext.PlayHistory.Add(entry);
        await dbContext.SaveChangesAsync(cancellationToken);
        dbContext.Entry(entry).State = EntityState.Detached;
    }

    public Task<Page<PlayHistoryEntry>> ListAsync(
        PlayHistoryListQuery query,
        CancellationToken cancellationToken = default)
    {
        return dbContext.PlayHistory.AsNoTracking()
            .Where(entry => entry.UserId == query.UserId)
            .OrderByDescending(entry => entry.PlayedAt)
            .ThenByDescending(entry => entry.Id)
            .ToPageAsync(query.Offset, query.Limit, cancellationToken);
    }

    public Task<PlayHistoryEntry?> FindAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.PlayHistory.AsNoTracking()
            .FirstOrDefaultAsync(entry => entry.Id == id && entry.UserId == userId, cancellationToken);
    }
}
