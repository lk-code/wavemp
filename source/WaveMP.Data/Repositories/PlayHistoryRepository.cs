using Microsoft.EntityFrameworkCore;
using WaveMP.Data.Entities;
using WaveMP.Data.Projections;
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

    public async Task<Page<RecentTrackRow>> ListRecentTracksAsync(
        RecentTrackListQuery query,
        CancellationToken cancellationToken = default)
    {
        var tracks = dbContext.PlayHistory.AsNoTracking()
            .Where(entry => entry.UserId == query.UserId)
            .GroupBy(entry => entry.TrackId)
            .Select(group => new { TrackId = group.Key, LastPlayedAt = group.Max(entry => entry.PlayedAt) });

        int total = Math.Min(await tracks.CountAsync(cancellationToken), Math.Max(0, query.MaxTracks));
        int take = Math.Min(query.Limit, total - query.Offset);
        if (take <= 0)
        {
            return new Page<RecentTrackRow>([], query.Offset, query.Limit, total);
        }

        List<RecentTrackRow> items = await tracks
            .OrderByDescending(track => track.LastPlayedAt)
            .ThenBy(track => track.TrackId)
            .Skip(query.Offset)
            .Take(take)
            .Select(track => new RecentTrackRow(track.TrackId, track.LastPlayedAt))
            .ToListAsync(cancellationToken);
        return new Page<RecentTrackRow>(items, query.Offset, query.Limit, total);
    }
}
