using Microsoft.EntityFrameworkCore;
using WaveMP.Data.Entities;
using WaveMP.Data.Projections;
using WaveMP.Data.Queries;

namespace WaveMP.Data.Repositories;

internal sealed class TrackRepository(WaveMpDbContext dbContext) : ITrackRepository
{
    /// <summary>
    /// Weights of a match of the artist and of the album against a match of the title in the relevance.
    /// </summary>
    private const double ArtistWeight = 0.9;
    private const double AlbumWeight = 0.8;

    private const string BinaryCollation = "C";

    public Task<Page<TrackRow>> ListAsync(TrackListQuery query, CancellationToken cancellationToken = default)
    {
        IQueryable<Track> tracks = Filter(dbContext.Tracks.AsNoTracking(), query);
        return Project(Sort(tracks, query).ThenBy(track => track.Id), query.UserId)
            .ToPageAsync(query.Offset, query.Limit, cancellationToken);
    }

    public Task<TrackRow?> FindAsync(Guid id, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        return Project(dbContext.Tracks.AsNoTracking().Where(track => track.Id == id), userId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Tracks.AnyAsync(track => track.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<TrackRow>> ListByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        Guid[] distinct = [.. ids.Distinct()];
        return await Project(dbContext.Tracks.AsNoTracking().Where(track => distinct.Contains(track.Id)), userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<TrackFileRow?> FindPrimaryFileAsync(
        Guid trackId,
        MediaFileKind kind,
        CancellationToken cancellationToken = default)
    {
        // Checks that the track exists and reads its offset in one query.
        int? offset = await dbContext.Tracks.AsNoTracking()
            .Where(track => track.Id == trackId)
            .Select(track => (int?)track.VideoOffsetMilliseconds)
            .FirstOrDefaultAsync(cancellationToken);
        if (offset is not { } videoOffset)
        {
            return null;
        }

        MediaFile? file = await dbContext.MediaFiles.AsNoTracking()
            .Where(file => file.TrackId == trackId && file.Kind == kind && file.IsPrimary)
            .OrderBy(file => file.Id)
            .FirstOrDefaultAsync(cancellationToken);
        return new TrackFileRow(trackId, file, videoOffset);
    }

    public async Task<bool> SetVideoOffsetAsync(
        Guid trackId,
        int milliseconds,
        CancellationToken cancellationToken = default)
    {
        // Not a change of the content of the library, so the time of the last change stays.
        int changed = await dbContext.Tracks
            .Where(track => track.Id == trackId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(track => track.VideoOffsetMilliseconds, milliseconds),
                cancellationToken);
        return changed > 0;
    }

    public Task<Page<MediaFile>> ListFilesAsync(
        TrackFileListQuery query,
        CancellationToken cancellationToken = default)
    {
        IQueryable<MediaFile> files = dbContext.MediaFiles.AsNoTracking().Where(file => file.TrackId == query.TrackId);
        if (query.Kind is { } kind)
        {
            files = files.Where(file => file.Kind == kind);
        }

        // The kind is stored as its name; its order comes from an expression, not from the text.
        IOrderedQueryable<MediaFile> sorted = query.Sort switch
        {
            MediaFileSortField.Name => files.OrderBy(file => file.FileName, query.Direction),
            MediaFileSortField.Size => files
                .OrderBy(file => file.Size, query.Direction)
                .ThenBy(file => file.FileName),
            _ => files
                .OrderBy(
                    file => file.Kind == MediaFileKind.Audio ? 0 : file.Kind == MediaFileKind.Video ? 1 : 2,
                    query.Direction)
                .ThenByDescending(file => file.IsPrimary)
                .ThenBy(file => file.FileName),
        };

        return sorted.ThenBy(file => file.Id).ToPageAsync(query.Offset, query.Limit, cancellationToken);
    }

    public Task<Page<GenreRow>> ListGenresAsync(GenreListQuery query, CancellationToken cancellationToken = default)
    {
        IQueryable<Track> tracks = MyMusicOf(dbContext.Tracks.AsNoTracking(), query.UserId, query.MyMusic)
            .Where(track => track.Genre != null);
        if (query.Search is { } search)
        {
            string pattern = LikePatterns.Contains(search);
            tracks = tracks.Where(track =>
                EF.Functions.ILike(track.Genre!, pattern, LikePatterns.EscapeCharacter)
                || EF.Functions.TrigramsAreWordSimilar(search, track.Genre!));
        }

        var genres = tracks
            .GroupBy(track => track.Genre!.ToUpper())
            .Select(group => new
            {
                group.Key,
                // Binary order, so the spelling with capitals wins independent of the collation of the database.
                Name = group.Min(track => EF.Functions.Collate(track.Genre!, BinaryCollation))!,
                TrackCount = group.Count(),
                AlbumCount = group
                    .Where(track => track.AlbumId != null)
                    .Select(track => track.AlbumId)
                    .Distinct()
                    .Count(),
            });

        var sorted = query.Sort == GenreSortField.TrackCount
            ? genres.OrderBy(genre => genre.TrackCount, query.Direction).ThenBy(genre => genre.Key)
            : genres.OrderBy(genre => genre.Key, query.Direction);

        return sorted
            .Select(genre => new GenreRow(genre.Name, genre.TrackCount, genre.AlbumCount))
            .ToPageAsync(query.Offset, query.Limit, cancellationToken);
    }

    public Task<Page<YearRow>> ListYearsAsync(YearListQuery query, CancellationToken cancellationToken = default)
    {
        return MyMusicOf(dbContext.Tracks.AsNoTracking(), query.UserId, query.MyMusic)
            .Where(track => track.Year != null)
            .GroupBy(track => track.Year!.Value)
            .Select(group => new
            {
                Year = group.Key,
                TrackCount = group.Count(),
                AlbumCount = group
                    .Where(track => track.AlbumId != null)
                    .Select(track => track.AlbumId)
                    .Distinct()
                    .Count(),
            })
            .OrderBy(year => year.Year, query.Direction)
            .Select(year => new YearRow(year.Year, year.TrackCount, year.AlbumCount))
            .ToPageAsync(query.Offset, query.Limit, cancellationToken);
    }

    private IQueryable<Track> Filter(IQueryable<Track> tracks, TrackListQuery query)
    {
        if (query.Search is { } search)
        {
            string pattern = LikePatterns.Contains(search);
            tracks = tracks.Where(track =>
                EF.Functions.ILike(track.Title, pattern, LikePatterns.EscapeCharacter)
                || EF.Functions.TrigramsAreWordSimilar(search, track.Title)
                || (track.Artist != null
                    && (EF.Functions.ILike(track.Artist.Name, pattern, LikePatterns.EscapeCharacter)
                        || EF.Functions.TrigramsAreWordSimilar(search, track.Artist.Name)))
                || (track.Album != null
                    && (EF.Functions.ILike(track.Album.Title, pattern, LikePatterns.EscapeCharacter)
                        || EF.Functions.TrigramsAreWordSimilar(search, track.Album.Title))));
        }

        if (query.ArtistId is { } artistId)
        {
            tracks = tracks.Where(track =>
                track.ArtistId == artistId || (track.Album != null && track.Album.AlbumArtistId == artistId));
        }

        if (query.AlbumId is { } albumId)
        {
            tracks = tracks.Where(track => track.AlbumId == albumId);
        }

        if (query.Genre is { } genre)
        {
            string genrePattern = LikePatterns.Escape(genre);
            tracks = tracks.Where(track =>
                track.Genre != null && EF.Functions.ILike(track.Genre, genrePattern, LikePatterns.EscapeCharacter));
        }

        if (query.Year is { } year)
        {
            tracks = tracks.Where(track => track.Year == year);
        }

        if (query.HasAudio is { } hasAudio)
        {
            tracks = tracks.Where(track => track.HasAudio == hasAudio);
        }

        if (query.HasVideo is { } hasVideo)
        {
            tracks = tracks.Where(track => track.HasVideo == hasVideo);
        }

        tracks = MyMusicOf(tracks, query.UserId, query.MyMusic);
        if (query.MinRating is { } minRating)
        {
            Guid userId = MyMusicQueries.RequireUser(query.UserId, "The rating filter");
            tracks = tracks.Where(track => dbContext.TrackRatings.Any(rating =>
                rating.UserId == userId && rating.TrackId == track.Id && rating.Rating >= minRating));
        }

        return tracks;
    }

    private IQueryable<Track> MyMusicOf(IQueryable<Track> tracks, Guid? userId, bool myMusic)
    {
        return myMusic
            ? MyMusicQueries.Filter(tracks, dbContext, MyMusicQueries.RequireUser(userId, "My music"))
            : tracks;
    }

    /// <summary>
    /// Sorts by the key in the direction; keys that group by album keep the tracks of an album together in the order
    /// of disc and track number, which always ascends.
    /// </summary>
    private IOrderedQueryable<Track> Sort(IQueryable<Track> tracks, TrackListQuery query)
    {
        SortDirection direction = query.Direction;
        string? search = query.Search;
        if (query.Sort == TrackSortField.Rating)
        {
            Guid userId = MyMusicQueries.RequireUser(query.UserId, "Sorting by rating");
            IQueryable<TrackRating> ratings = dbContext.TrackRatings.Where(rating => rating.UserId == userId);

            // Unrated tracks count as 0; ties by the time of the rating, the latest first.
            return ThenByAlbum(tracks
                .OrderBy(
                    track => ratings
                        .Where(rating => rating.TrackId == track.Id)
                        .Select(rating => rating.Rating)
                        .FirstOrDefault(),
                    direction)
                .ThenByDescending(track => ratings
                    .Where(rating => rating.TrackId == track.Id)
                    .Select(rating => (DateTimeOffset?)rating.UpdatedAt)
                    .FirstOrDefault()));
        }

        return query.Sort switch
        {
            TrackSortField.Album => ThenByAlbum(tracks.OrderBy(track => track.Album!.NormalizedTitle, direction)),
            TrackSortField.Title => tracks.OrderBy(track => track.Title, direction),
            TrackSortField.Artist => ThenByAlbum(tracks
                .OrderBy(track => track.Artist!.NormalizedName, direction)
                .ThenBy(track => track.Album!.NormalizedTitle)),
            TrackSortField.Duration => tracks.OrderBy(track => track.Duration, direction),
            TrackSortField.Year => ThenByAlbum(tracks
                .OrderBy(track => track.Year, direction)
                .ThenBy(track => track.Album!.NormalizedTitle)),
            TrackSortField.Genre => ThenByAlbum(tracks
                .OrderBy(track => track.Genre, direction)
                .ThenBy(track => track.Album!.NormalizedTitle)),
            // Tracks of one folder are found at the same time, so albums stay together.
            TrackSortField.FirstSeenAt => ThenByAlbum(tracks.OrderBy(track => track.FirstSeenAt, direction)),
            TrackSortField.Relevance when search is not null => tracks
                .OrderBy(
                    track => Math.Max(
                        EF.Functions.TrigramsWordSimilarity(search, track.Title),
                        Math.Max(
                            ArtistWeight * EF.Functions.TrigramsWordSimilarity(search, track.Artist!.Name),
                            AlbumWeight * EF.Functions.TrigramsWordSimilarity(search, track.Album!.Title))),
                    direction)
                .ThenBy(track => track.Title),
            // Album artist; tracks without album by their own artist. Compilations and unknown artists come last.
            _ => ThenByAlbum(tracks
                .OrderBy(
                    track => track.Album != null
                        ? track.Album.AlbumArtist!.NormalizedName
                        : track.Artist!.NormalizedName,
                    direction)
                .ThenBy(track => track.Album!.NormalizedTitle)),
        };
    }

    private static IOrderedQueryable<Track> ThenByAlbum(IOrderedQueryable<Track> sorted)
    {
        return sorted
            .ThenBy(track => track.AlbumId)
            .ThenBy(track => track.DiscNumber)
            .ThenBy(track => track.TrackNumber)
            .ThenBy(track => track.Title);
    }

    /// <summary>
    /// A track has a cover if it has its own, its album has one or another track of its album has one, the same
    /// covers the cover of a track falls back to. Favorite and rating are those of the user; without a user, no track
    /// is a favorite or rated.
    /// </summary>
    private IQueryable<TrackRow> Project(IQueryable<Track> tracks, Guid? userId)
    {
        // No user has the empty id, so the subqueries find nothing without a user.
        Guid user = userId ?? Guid.Empty;
        return tracks.Select(track => new TrackRow(
            track.Id,
            track.Title,
            track.ArtistId,
            track.Artist != null ? track.Artist.Name : null,
            track.AlbumId,
            track.Album != null ? track.Album.Title : null,
            track.Album != null ? track.Album.AlbumArtistId : null,
            track.Album != null && track.Album.AlbumArtist != null ? track.Album.AlbumArtist.Name : null,
            track.Album != null && track.Album.IsCompilation,
            track.TrackNumber,
            track.DiscNumber,
            track.Year,
            track.Genre,
            track.Duration,
            track.HasAudio,
            track.HasVideo,
            track.Covers.Any()
                || (track.AlbumId != null
                    && dbContext.Covers.Any(cover =>
                        cover.AlbumId == track.AlbumId
                        || (cover.Track != null && cover.Track.AlbumId == track.AlbumId))),
            track.FirstSeenAt,
            dbContext.Favorites.Any(favorite => favorite.UserId == user && favorite.TrackId == track.Id),
            dbContext.TrackRatings
                .Where(rating => rating.UserId == user && rating.TrackId == track.Id)
                .Select(rating => rating.Rating)
                .FirstOrDefault()));
    }
}
