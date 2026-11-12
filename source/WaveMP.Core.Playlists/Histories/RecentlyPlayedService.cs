using Microsoft.Extensions.Options;
using WaveMP.Core.Playlists.Options;
using WaveMP.Data.Projections;
using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Playlists.Histories;

internal sealed class RecentlyPlayedService(
    IPlayHistoryRepository history,
    ITrackRepository tracks,
    IOptions<PlaylistOptions> options) : IRecentlyPlayedService
{
    public async Task<Page<RecentlyPlayedItem>> ListAsync(
        Guid userId,
        int offset,
        int limit,
        CancellationToken cancellationToken = default)
    {
        Page<RecentTrackRow> page = await history.ListRecentTracksAsync(
            new RecentTrackListQuery(userId, offset, limit, options.Value.RecentlyPlayedMaxTracks),
            cancellationToken);
        IReadOnlyList<TrackRow> rows = await tracks.ListByIdsAsync(
            [.. page.Items.Select(item => item.TrackId)],
            userId,
            cancellationToken);
        var byId = rows.ToDictionary(row => row.Id);

        // A track removed by a scan in the meantime takes its history with it; it is left out.
        List<RecentlyPlayedItem> items = [.. page.Items
            .Where(item => byId.ContainsKey(item.TrackId))
            .Select(item => new RecentlyPlayedItem(byId[item.TrackId], item.LastPlayedAt))];
        return new Page<RecentlyPlayedItem>(items, page.Offset, page.Limit, page.Total);
    }
}
