using Microsoft.EntityFrameworkCore;
using WaveMP.Data.Entities;
using WaveMP.Data.Projections;
using WaveMP.Data.Queries;

namespace WaveMP.Data.Repositories;

internal sealed class AlbumRepository(WaveMpDbContext dbContext) : IAlbumRepository
{
    /// <summary>
    /// Weight of a match of the album artist against a match of the title in the relevance.
    /// </summary>
    private const double ArtistWeight = 0.9;

    public Task<Page<AlbumRow>> ListAsync(AlbumListQuery query, CancellationToken cancellationToken = default)
    {
        IQueryable<Album> albums = dbContext.Albums.AsNoTracking();
        string? search = query.Search;
        if (search is not null)
        {
            string pattern = LikePatterns.Contains(search);
            albums = albums.Where(album =>
                EF.Functions.ILike(album.Title, pattern, LikePatterns.EscapeCharacter)
                || EF.Functions.TrigramsAreWordSimilar(search, album.Title)
                || (album.AlbumArtist != null
                    && (EF.Functions.ILike(album.AlbumArtist.Name, pattern, LikePatterns.EscapeCharacter)
                        || EF.Functions.TrigramsAreWordSimilar(search, album.AlbumArtist.Name))));
        }

        if (query.ArtistId is { } artistId)
        {
            albums = albums.Where(album =>
                album.AlbumArtistId == artistId || album.Tracks.Any(track => track.ArtistId == artistId));
        }

        if (query.Genre is { } genre)
        {
            string genrePattern = LikePatterns.Escape(genre);
            albums = albums.Where(album => album.Tracks.Any(track =>
                track.Genre != null && EF.Functions.ILike(track.Genre, genrePattern, LikePatterns.EscapeCharacter)));
        }

        if (query.Year is { } year)
        {
            albums = albums.Where(album => album.Year == year);
        }

        if (query.MyMusic)
        {
            IQueryable<Track> myTracks = MyMusicQueries.Tracks(
                dbContext,
                MyMusicQueries.RequireUser(query.UserId, "My music"));
            albums = albums.Where(album => myTracks.Any(track => track.AlbumId == album.Id));
        }

        SortDirection direction = query.Direction;
        IOrderedQueryable<Album> sorted = query.Sort switch
        {
            AlbumSortField.Title => albums.OrderBy(album => album.NormalizedTitle, direction),
            AlbumSortField.Year => albums
                .OrderBy(album => album.Year, direction)
                .ThenBy(album => album.NormalizedTitle),
            AlbumSortField.FirstSeenAt => albums
                .OrderBy(album => album.FirstSeenAt, direction)
                .ThenBy(album => album.NormalizedTitle),
            AlbumSortField.Relevance when search is not null => albums
                .OrderBy(
                    album => Math.Max(
                        EF.Functions.TrigramsWordSimilarity(search, album.Title),
                        ArtistWeight * EF.Functions.TrigramsWordSimilarity(search, album.AlbumArtist!.Name)),
                    direction)
                .ThenBy(album => album.NormalizedTitle),
            _ => albums
                .OrderBy(album => album.AlbumArtist!.NormalizedName, direction)
                .ThenBy(album => album.NormalizedTitle),
        };

        return Project(sorted.ThenBy(album => album.Id), query.UserId)
            .ToPageAsync(query.Offset, query.Limit, cancellationToken);
    }

    public Task<AlbumRow?> FindAsync(Guid id, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        return Project(dbContext.Albums.AsNoTracking().Where(album => album.Id == id), userId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AlbumRow>> ListByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        Guid[] distinct = [.. ids.Distinct()];
        return await Project(dbContext.Albums.AsNoTracking().Where(album => distinct.Contains(album.Id)), userId)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Albums.AnyAsync(album => album.Id == id, cancellationToken);
    }

    /// <summary>
    /// The genre of an album is the first genre of its tracks in the order of disc and track number, its duration the
    /// sum of the durations of its tracks. Without a user, no album is a favorite.
    /// </summary>
    private IQueryable<AlbumRow> Project(IQueryable<Album> albums, Guid? userId)
    {
        Guid user = userId ?? Guid.Empty;
        return albums.Select(album => new AlbumRow(
            album.Id,
            album.Title,
            album.AlbumArtistId,
            album.AlbumArtist != null ? album.AlbumArtist.Name : null,
            album.IsCompilation,
            album.Year,
            album.Tracks
                .Where(track => track.Genre != null)
                .OrderBy(track => track.DiscNumber)
                .ThenBy(track => track.TrackNumber)
                .ThenBy(track => track.Id)
                .Select(track => track.Genre)
                .FirstOrDefault(),
            album.Tracks.Count,
            album.Tracks.Sum(track => track.Duration.HasValue ? track.Duration.Value.TotalMilliseconds : 0),
            dbContext.Covers.Any(cover =>
                cover.AlbumId == album.Id || (cover.Track != null && cover.Track.AlbumId == album.Id)),
            album.FirstSeenAt,
            dbContext.Favorites.Any(favorite => favorite.UserId == user && favorite.AlbumId == album.Id)));
    }
}
