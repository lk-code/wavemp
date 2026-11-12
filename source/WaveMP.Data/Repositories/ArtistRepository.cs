using Microsoft.EntityFrameworkCore;
using WaveMP.Data.Entities;
using WaveMP.Data.Projections;
using WaveMP.Data.Queries;

namespace WaveMP.Data.Repositories;

internal sealed class ArtistRepository(WaveMpDbContext dbContext) : IArtistRepository
{
    public Task<Page<ArtistRow>> ListAsync(ArtistListQuery query, CancellationToken cancellationToken = default)
    {
        IQueryable<Artist> artists = dbContext.Artists.AsNoTracking();
        string? search = query.Search;
        if (search is not null)
        {
            string pattern = LikePatterns.Contains(search);
            artists = artists.Where(artist =>
                EF.Functions.ILike(artist.Name, pattern, LikePatterns.EscapeCharacter)
                || EF.Functions.TrigramsAreWordSimilar(search, artist.Name));
        }

        if (query.MyMusic)
        {
            // Like the artist filter of the track list: as artist of the track or of its album.
            IQueryable<Track> myTracks = MyMusicQueries.Tracks(
                dbContext,
                MyMusicQueries.RequireUser(query.UserId, "My music"));
            artists = artists.Where(artist => myTracks.Any(track =>
                track.ArtistId == artist.Id || (track.Album != null && track.Album.AlbumArtistId == artist.Id)));
        }

        IOrderedQueryable<Artist> sorted = query.Sort switch
        {
            ArtistSortField.FirstSeenAt => artists
                .OrderBy(artist => artist.FirstSeenAt, query.Direction)
                .ThenBy(artist => artist.NormalizedName),
            ArtistSortField.Relevance when search is not null => artists
                .OrderBy(artist => EF.Functions.TrigramsWordSimilarity(search, artist.Name), query.Direction)
                .ThenBy(artist => artist.NormalizedName),
            _ => artists.OrderBy(artist => artist.NormalizedName, query.Direction),
        };

        return Project(sorted.ThenBy(artist => artist.Id), query.UserId)
            .ToPageAsync(query.Offset, query.Limit, cancellationToken);
    }

    public Task<ArtistRow?> FindAsync(Guid id, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        return Project(dbContext.Artists.AsNoTracking().Where(artist => artist.Id == id), userId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ArtistRow>> ListByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        Guid[] distinct = [.. ids.Distinct()];
        return await Project(dbContext.Artists.AsNoTracking().Where(artist => distinct.Contains(artist.Id)), userId)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Artists.AnyAsync(artist => artist.Id == id, cancellationToken);
    }

    /// <summary>
    /// Albums count with the artist as album artist or with tracks of the artist, tracks with the artist or on albums
    /// of the artist, like the artist filters of the album and track lists. Without a user, no artist is a favorite.
    /// </summary>
    private IQueryable<ArtistRow> Project(IQueryable<Artist> artists, Guid? userId)
    {
        Guid user = userId ?? Guid.Empty;
        return artists.Select(artist => new ArtistRow(
            artist.Id,
            artist.Name,
            dbContext.Albums.Count(album =>
                album.AlbumArtistId == artist.Id || album.Tracks.Any(track => track.ArtistId == artist.Id)),
            dbContext.Tracks.Count(track =>
                track.ArtistId == artist.Id || (track.Album != null && track.Album.AlbumArtistId == artist.Id)),
            artist.FirstSeenAt,
            dbContext.Favorites.Any(favorite => favorite.UserId == user && favorite.ArtistId == artist.Id)));
    }
}
