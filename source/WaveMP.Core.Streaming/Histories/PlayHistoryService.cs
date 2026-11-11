using Microsoft.Extensions.Options;
using WaveMP.Core.Streaming.Options;
using WaveMP.Data.Entities;
using WaveMP.Data.Projections;
using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Streaming.Histories;

internal sealed class PlayHistoryService(
    IPlayHistoryRepository history,
    ITrackRepository tracks,
    TimeProvider timeProvider,
    IOptions<StreamingOptions> options) : IPlayHistoryService
{
    public async Task<PlayHistoryResult> RecordAsync(
        Guid userId,
        Guid trackId,
        DateTimeOffset? playedAt,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        DateTimeOffset start = playedAt?.ToUniversalTime() ?? now;
        StreamingOptions settings = options.Value;
        if (start > now + settings.MaxClockSkew || start < now - settings.HistoryMaxAge)
        {
            return new PlayHistoryResult(PlayHistoryStatus.InvalidPlayedAt, null);
        }

        TrackRow? track = await tracks.FindAsync(trackId, cancellationToken);
        if (track is null)
        {
            return new PlayHistoryResult(PlayHistoryStatus.TrackNotFound, null);
        }

        PlayHistoryEntry entry = new()
        {
            Id = Guid.CreateVersion7(now),
            UserId = userId,
            TrackId = trackId,
            PlayedAt = start,
        };
        await history.AddAsync(entry, cancellationToken);
        return new PlayHistoryResult(PlayHistoryStatus.Created, new PlayHistoryItem(entry, track));
    }

    public async Task<Page<PlayHistoryItem>> ListAsync(
        Guid userId,
        int offset,
        int limit,
        CancellationToken cancellationToken = default)
    {
        Page<PlayHistoryEntry> page = await history.ListAsync(
            new PlayHistoryListQuery(userId, offset, limit),
            cancellationToken);
        IReadOnlyList<TrackRow> rows = await tracks.ListByIdsAsync(
            [.. page.Items.Select(entry => entry.TrackId)],
            cancellationToken);
        var byId = rows.ToDictionary(row => row.Id);

        // A track removed by a scan in the meantime takes its entries with it (cascade); they are left out.
        List<PlayHistoryItem> items = [.. page.Items
            .Where(entry => byId.ContainsKey(entry.TrackId))
            .Select(entry => new PlayHistoryItem(entry, byId[entry.TrackId]))];
        return new Page<PlayHistoryItem>(items, page.Offset, page.Limit, page.Total);
    }

    public async Task<PlayHistoryItem?> GetAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)
    {
        PlayHistoryEntry? entry = await history.FindAsync(userId, id, cancellationToken);
        if (entry is null)
        {
            return null;
        }

        TrackRow? track = await tracks.FindAsync(entry.TrackId, cancellationToken);
        return track is null ? null : new PlayHistoryItem(entry, track);
    }
}
