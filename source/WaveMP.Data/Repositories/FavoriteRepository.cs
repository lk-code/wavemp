using Microsoft.EntityFrameworkCore;
using Npgsql;
using WaveMP.Data.Entities;
using WaveMP.Data.Projections;
using WaveMP.Data.Queries;

namespace WaveMP.Data.Repositories;

internal sealed class FavoriteRepository(WaveMpDbContext dbContext) : IFavoriteRepository
{
    public async Task<FavoriteChangeStatus> AddAsync(
        Favorite favorite,
        CancellationToken cancellationToken = default)
    {
        Guid itemId = ItemIdOf(favorite);
        if (!await TargetExistsAsync(favorite.Type, itemId, cancellationToken))
        {
            return FavoriteChangeStatus.TargetNotFound;
        }

        string type = favorite.Type.ToString();
        DateTimeOffset createdAt = favorite.CreatedAt.ToUniversalTime();
        try
        {
            // The unique indexes of user and target keep an existing favorite with its time.
            int added = await dbContext.Database.ExecuteSqlAsync(
                $"""
                INSERT INTO favorites (id, user_id, type, track_id, album_id, artist_id, created_at)
                VALUES ({favorite.Id}, {favorite.UserId}, {type}, {favorite.TrackId}, {favorite.AlbumId},
                    {favorite.ArtistId}, {createdAt})
                ON CONFLICT DO NOTHING
                """,
                cancellationToken);
            return added > 0 ? FavoriteChangeStatus.Added : FavoriteChangeStatus.Existing;
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            // A scan removed the target in the meantime.
            return FavoriteChangeStatus.TargetNotFound;
        }
    }

    public async Task<bool> RemoveAsync(
        Guid userId,
        FavoriteType type,
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Favorite> favorites = dbContext.Favorites.Where(favorite => favorite.UserId == userId);
        favorites = type switch
        {
            FavoriteType.Track => favorites.Where(favorite => favorite.TrackId == itemId),
            FavoriteType.Album => favorites.Where(favorite => favorite.AlbumId == itemId),
            FavoriteType.Artist => favorites.Where(favorite => favorite.ArtistId == itemId),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown favorite type."),
        };

        return await favorites.ExecuteDeleteAsync(cancellationToken) > 0;
    }

    public Task<Page<FavoriteRow>> ListAsync(FavoriteListQuery query, CancellationToken cancellationToken = default)
    {
        IQueryable<Favorite> favorites = dbContext.Favorites.AsNoTracking()
            .Where(favorite => favorite.UserId == query.UserId);
        if (query.Type is { } type)
        {
            favorites = favorites.Where(favorite => favorite.Type == type);
        }

        return favorites
            .OrderByDescending(favorite => favorite.CreatedAt)
            .ThenByDescending(favorite => favorite.Id)
            .Select(favorite => new FavoriteRow(
                favorite.Id,
                favorite.Type,
                favorite.TrackId ?? favorite.AlbumId ?? favorite.ArtistId ?? Guid.Empty,
                favorite.CreatedAt))
            .ToPageAsync(query.Offset, query.Limit, cancellationToken);
    }

    private Task<bool> TargetExistsAsync(FavoriteType type, Guid itemId, CancellationToken cancellationToken)
    {
        return type switch
        {
            FavoriteType.Track => dbContext.Tracks.AnyAsync(track => track.Id == itemId, cancellationToken),
            FavoriteType.Album => dbContext.Albums.AnyAsync(album => album.Id == itemId, cancellationToken),
            FavoriteType.Artist => dbContext.Artists.AnyAsync(artist => artist.Id == itemId, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown favorite type."),
        };
    }

    /// <summary>
    /// The id of the type; the other ids must be empty, as the check constraint of the table requires.
    /// </summary>
    private static Guid ItemIdOf(Favorite favorite)
    {
        Guid? itemId = favorite.Type switch
        {
            FavoriteType.Track when favorite.AlbumId is null && favorite.ArtistId is null => favorite.TrackId,
            FavoriteType.Album when favorite.TrackId is null && favorite.ArtistId is null => favorite.AlbumId,
            FavoriteType.Artist when favorite.TrackId is null && favorite.AlbumId is null => favorite.ArtistId,
            _ => null,
        };
        return itemId ?? throw new ArgumentException("A favorite needs exactly the id of its type.", nameof(favorite));
    }
}
