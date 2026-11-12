using WaveMP.Data.Entities;
using WaveMP.Data.Projections;
using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Collection.Favorites;

internal sealed class FavoriteService(
    IFavoriteRepository favorites,
    ITrackRepository tracks,
    IAlbumRepository albums,
    IArtistRepository artists,
    TimeProvider timeProvider) : IFavoriteService
{
    public async Task<FavoriteStatus> AddAsync(
        Guid userId,
        FavoriteType type,
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = TruncateToMicroseconds(timeProvider.GetUtcNow());
        Favorite favorite = new()
        {
            Id = Guid.CreateVersion7(now),
            UserId = userId,
            Type = type,
            TrackId = type == FavoriteType.Track ? itemId : null,
            AlbumId = type == FavoriteType.Album ? itemId : null,
            ArtistId = type == FavoriteType.Artist ? itemId : null,
            CreatedAt = now,
        };

        FavoriteChangeStatus status = await favorites.AddAsync(favorite, cancellationToken);
        return status == FavoriteChangeStatus.TargetNotFound ? FavoriteStatus.NotFound : FavoriteStatus.Done;
    }

    public Task RemoveAsync(
        Guid userId,
        FavoriteType type,
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        return favorites.RemoveAsync(userId, type, itemId, cancellationToken);
    }

    public async Task<Page<FavoriteItem>> ListAsync(
        FavoriteListRequest request,
        CancellationToken cancellationToken = default)
    {
        Page<FavoriteRow> page = await favorites.ListAsync(
            new FavoriteListQuery(request.UserId, request.Offset, request.Limit, request.Type),
            cancellationToken);

        // One after the other: the repositories share the database context of the request.
        var trackRows = (await tracks.ListByIdsAsync(
                IdsOf(page, FavoriteType.Track),
                request.UserId,
                cancellationToken))
            .ToDictionary(row => row.Id);
        var albumRows = (await albums.ListByIdsAsync(
                IdsOf(page, FavoriteType.Album),
                request.UserId,
                cancellationToken))
            .ToDictionary(row => row.Id);
        var artistRows = (await artists.ListByIdsAsync(
                IdsOf(page, FavoriteType.Artist),
                request.UserId,
                cancellationToken))
            .ToDictionary(row => row.Id);

        // A target removed by a scan in the meantime takes its favorite with it; it is left out.
        List<FavoriteItem> items = [];
        foreach (FavoriteRow row in page.Items)
        {
            FavoriteItem? item = row.Type switch
            {
                FavoriteType.Track when trackRows.TryGetValue(row.ItemId, out TrackRow? track) =>
                    new FavoriteItem(row, track, null, null),
                FavoriteType.Album when albumRows.TryGetValue(row.ItemId, out AlbumRow? album) =>
                    new FavoriteItem(row, null, album, null),
                FavoriteType.Artist when artistRows.TryGetValue(row.ItemId, out ArtistRow? artist) =>
                    new FavoriteItem(row, null, null, artist),
                _ => null,
            };
            if (item is not null)
            {
                items.Add(item);
            }
        }

        return new Page<FavoriteItem>(items, page.Offset, page.Limit, page.Total);
    }

    private static Guid[] IdsOf(Page<FavoriteRow> page, FavoriteType type)
    {
        return [.. page.Items.Where(row => row.Type == type).Select(row => row.ItemId)];
    }

    /// <summary>
    /// PostgreSQL keeps microseconds; times are created with the precision they are read with later.
    /// </summary>
    private static DateTimeOffset TruncateToMicroseconds(DateTimeOffset value)
    {
        return new DateTimeOffset(value.Ticks - (value.Ticks % TimeSpan.TicksPerMicrosecond), value.Offset);
    }
}
