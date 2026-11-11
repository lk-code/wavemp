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

        return Project(sorted.ThenBy(artist => artist.Id)).ToPageAsync(query.Offset, query.Limit, cancellationToken);
    }

    public Task<ArtistRow?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Project(dbContext.Artists.AsNoTracking().Where(artist => artist.Id == id))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Artists.AnyAsync(artist => artist.Id == id, cancellationToken);
    }

    /// <summary>
    /// Albums count with the artist as album artist or with tracks of the artist, tracks with the artist or on albums
    /// of the artist, like the artist filters of the album and track lists.
    /// </summary>
    private IQueryable<ArtistRow> Project(IQueryable<Artist> artists)
    {
        return artists.Select(artist => new ArtistRow(
            artist.Id,
            artist.Name,
            dbContext.Albums.Count(album =>
                album.AlbumArtistId == artist.Id || album.Tracks.Any(track => track.ArtistId == artist.Id)),
            dbContext.Tracks.Count(track =>
                track.ArtistId == artist.Id || (track.Album != null && track.Album.AlbumArtistId == artist.Id)),
            artist.FirstSeenAt));
    }
}
