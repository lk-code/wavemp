using WaveMP.Data.Queries;
using WaveMP.Data.Repositories;

namespace WaveMP.Core.Library.Queries;

/// <summary>
/// The sort keys and directions that apply when a client sends none. With a search text the relevance comes first;
/// the relevance without a search text falls back to the default key. Keys for which the largest value is the most
/// interesting (relevance, time found first, rating, number of tracks) sort descending by default, all others
/// ascending.
/// </summary>
public static class LibraryQueryDefaults
{
    /// <summary>
    /// The sort key of the tracks. The rating belongs to a profile; without one, it falls back like the relevance
    /// without a search text.
    /// </summary>
    public static TrackSortField TrackSort(
        TrackSortField? sort,
        bool hasSearch,
        TrackSortField fallback = TrackSortField.AlbumArtist,
        bool hasUser = true)
    {
        TrackSortField key = sort ?? (hasSearch ? TrackSortField.Relevance : fallback);
        return (key == TrackSortField.Relevance && !hasSearch) || (key == TrackSortField.Rating && !hasUser)
            ? fallback
            : key;
    }

    public static AlbumSortField AlbumSort(AlbumSortField? sort, bool hasSearch)
    {
        AlbumSortField key = sort ?? (hasSearch ? AlbumSortField.Relevance : AlbumSortField.Artist);
        return key == AlbumSortField.Relevance && !hasSearch ? AlbumSortField.Artist : key;
    }

    public static ArtistSortField ArtistSort(ArtistSortField? sort, bool hasSearch)
    {
        ArtistSortField key = sort ?? (hasSearch ? ArtistSortField.Relevance : ArtistSortField.Name);
        return key == ArtistSortField.Relevance && !hasSearch ? ArtistSortField.Name : key;
    }

    public static GenreSortField GenreSort(GenreSortField? sort)
    {
        return sort ?? GenreSortField.Name;
    }

    public static MediaFileSortField FileSort(MediaFileSortField? sort)
    {
        return sort ?? MediaFileSortField.Kind;
    }

    public static SortDirection Direction(TrackSortField sort, SortDirection? direction)
    {
        return direction ?? Descending(
            sort is TrackSortField.FirstSeenAt or TrackSortField.Relevance or TrackSortField.Rating);
    }

    public static SortDirection Direction(AlbumSortField sort, SortDirection? direction)
    {
        return direction ?? Descending(sort is AlbumSortField.FirstSeenAt or AlbumSortField.Relevance);
    }

    public static SortDirection Direction(ArtistSortField sort, SortDirection? direction)
    {
        return direction ?? Descending(sort is ArtistSortField.FirstSeenAt or ArtistSortField.Relevance);
    }

    public static SortDirection Direction(GenreSortField sort, SortDirection? direction)
    {
        return direction ?? Descending(sort == GenreSortField.TrackCount);
    }

    /// <summary>
    /// The files of a track ascend by default.
    /// </summary>
    public static SortDirection FileDirection(SortDirection? direction)
    {
        return direction ?? SortDirection.Asc;
    }

    /// <summary>
    /// Years show the newest first by default.
    /// </summary>
    public static SortDirection YearDirection(SortDirection? direction)
    {
        return direction ?? SortDirection.Desc;
    }

    private static SortDirection Descending(bool descending)
    {
        return descending ? SortDirection.Desc : SortDirection.Asc;
    }
}
