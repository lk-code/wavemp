using WaveMP.Core.Library.Albums;
using WaveMP.Core.Library.Artists;
using WaveMP.Core.Library.Searches;
using WaveMP.Core.Library.Tracks;
using WaveMP.Core.Metadata.Texts;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Library.Queries;

/// <summary>
/// Turns the requests of the clients into queries of the repositories: cleans search text and genre and applies the
/// defaults of <see cref="LibraryQueryDefaults"/>.
/// </summary>
internal static class LibraryQueries
{
    public static ArtistListQuery ToQuery(ArtistListRequest request)
    {
        string? search = SearchText.Normalize(request.Search);
        ArtistSortField sort = LibraryQueryDefaults.ArtistSort(request.Sort, search is not null);
        return new ArtistListQuery(
            request.Offset,
            request.Limit,
            sort,
            LibraryQueryDefaults.Direction(sort, request.Direction),
            search,
            request.UserId,
            IsMyMusic(request.UserId, request.MyMusic));
    }

    public static AlbumListQuery ToQuery(AlbumListRequest request)
    {
        string? search = SearchText.Normalize(request.Search);
        AlbumSortField sort = LibraryQueryDefaults.AlbumSort(request.Sort, search is not null);
        return new AlbumListQuery(
            request.Offset,
            request.Limit,
            sort,
            LibraryQueryDefaults.Direction(sort, request.Direction),
            search,
            request.ArtistId,
            MetadataText.Normalize(request.Genre),
            request.Year,
            request.UserId,
            IsMyMusic(request.UserId, request.MyMusic));
    }

    /// <param name="request">The request of the client.</param>
    /// <param name="fallback">Sort key without a search text, if the request has none.</param>
    public static TrackListQuery ToQuery(TrackListRequest request, TrackSortField fallback)
    {
        string? search = SearchText.Normalize(request.Search);
        TrackSortField sort = LibraryQueryDefaults.TrackSort(
            request.Sort,
            search is not null,
            fallback,
            request.UserId is not null);
        return new TrackListQuery(
            request.Offset,
            request.Limit,
            sort,
            LibraryQueryDefaults.Direction(sort, request.Direction),
            search,
            request.ArtistId,
            request.AlbumId,
            MetadataText.Normalize(request.Genre),
            request.Year,
            request.HasAudio,
            request.HasVideo,
            request.UserId,
            IsMyMusic(request.UserId, request.MyMusic));
    }

    public static GenreListQuery ToQuery(GenreListRequest request)
    {
        GenreSortField sort = LibraryQueryDefaults.GenreSort(request.Sort);
        return new GenreListQuery(
            request.Offset,
            request.Limit,
            sort,
            LibraryQueryDefaults.Direction(sort, request.Direction),
            SearchText.Normalize(request.Search),
            request.UserId,
            IsMyMusic(request.UserId, request.MyMusic));
    }

    public static YearListQuery ToQuery(YearListRequest request)
    {
        return new YearListQuery(
            request.Offset,
            request.Limit,
            LibraryQueryDefaults.YearDirection(request.Direction),
            request.UserId,
            IsMyMusic(request.UserId, request.MyMusic));
    }

    /// <summary>
    /// "My music" belongs to a profile; without one, the filter does not apply.
    /// </summary>
    private static bool IsMyMusic(Guid? userId, bool myMusic)
    {
        return myMusic && userId is not null;
    }

    public static TrackFileListQuery ToQuery(Guid trackId, TrackFileListRequest request)
    {
        return new TrackFileListQuery(
            trackId,
            request.Offset,
            request.Limit,
            LibraryQueryDefaults.FileSort(request.Sort),
            LibraryQueryDefaults.FileDirection(request.Direction),
            request.Kind);
    }
}
